using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.Services;
using EasySave.Core.Strategies;
using EasySave.Core.Utils;

namespace EasySave.Core.ViewModels
{
    // ===== WPF VIEW MODEL (V2.0) =====
    // Differences from MainViewModel (V1.0/1.1):
    //   - No MaxJobs limit (#6)
    //   - Business software detection (#12, #13, #14, #15)
    //   - INotifyPropertyChanged for WPF data binding
    //   - Observable collections for UI reactivity
    public class WpfViewModel : INotifyPropertyChanged
    {
        // ===== PRIVATE MEMBERS =====
        private List<BackupJob> _jobs;
        private readonly string _configPath;
        private readonly LanguageManager _languageManager;
        private readonly IStateService _stateService;
        private readonly BusinessSoftwareMonitor _monitor;
        private CancellationTokenSource? _currentCts;

        private string _businessSoftwareName = "CalculatorApp";
        private bool _isBusinessSoftwareDetected;
        private bool _isExecuting;
        private string _statusMessage = string.Empty;

        // ===== OBSERVABLE COLLECTION FOR WPF =====
        public ObservableCollection<BackupJob> Jobs { get; }

        // ===== EVENTS =====
        public event PropertyChangedEventHandler? PropertyChanged;

        // ===== BINDABLE PROPERTIES =====

        public bool IsBusinessSoftwareDetected
        {
            get => _isBusinessSoftwareDetected;
            private set { _isBusinessSoftwareDetected = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public bool IsExecuting
        {
            get => _isExecuting;
            private set { _isExecuting = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanExecute)); }
        }

        public bool CanExecute => !IsBusinessSoftwareDetected && !IsExecuting;

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string BusinessSoftwareName
        {
            get => _businessSoftwareName;
            set
            {
                _businessSoftwareName = value;
                _monitor.ProcessName = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }

        // ===== CONSTRUCTOR =====
        public WpfViewModel(LanguageManager languageManager)
        {
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _stateService = new StateService();
            _jobs = new List<BackupJob>();
            Jobs = new ObservableCollection<BackupJob>();

            // Config path setup
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "EasySave");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");

            // Load jobs
            LoadJobs();
            RefreshObservableJobs();

            // Load settings (business software name)
            LoadSettings();

            // Business software monitor (#12)
            _monitor = BusinessSoftwareMonitor.Instance;
            _monitor.ProcessName = _businessSoftwareName;
            _monitor.DetectionChanged += OnBusinessSoftwareDetectionChanged;
            _monitor.Start(2000);
        }

        // ===== LANGUAGE =====
        public LanguageManager GetLanguageManager() => _languageManager;

        public void SetLanguage(Language lang)
        {
            _languageManager.SetLanguage(lang);
            OnPropertyChanged(nameof(Jobs)); // Refresh display
        }

        // ===== JOB MANAGEMENT (NO MAX LIMIT - #6) =====

        public int GetJobCount() => _jobs.Count;

        public bool CreateJob(string name, string source, string target, int typeInput)
        {
            // #6 - NO MaxJobs validation anymore

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!FileUtils.DirectoryExists(source))
                return false;

            BackupType type = typeInput == 2 ? BackupType.Differential : BackupType.Full;

            var job = new BackupJob(name, source, target, type);
            _jobs.Add(job);
            SaveJobs();
            RefreshObservableJobs();

            StatusMessage = _languageManager.GetText("job_created");
            return true;
        }

        public bool DeleteJob(BackupJob job)
        {
            if (job == null) return false;

            _jobs.Remove(job);
            SaveJobs();
            RefreshObservableJobs();

            StatusMessage = _languageManager.GetText("job_deleted");
            return true;
        }

        public bool DeleteJobByIndex(int zeroBasedIndex)
        {
            if (zeroBasedIndex < 0 || zeroBasedIndex >= _jobs.Count)
                return false;

            _jobs.RemoveAt(zeroBasedIndex);
            SaveJobs();
            RefreshObservableJobs();

            StatusMessage = _languageManager.GetText("job_deleted");
            return true;
        }

        // ===== EXECUTION WITH BUSINESS SOFTWARE CHECK (#13, #14) =====

        public void ExecuteJob(BackupJob job)
        {
            if (job == null) return;

            // #13 - Block if business software detected
            if (_monitor.CheckNow())
            {
                IsBusinessSoftwareDetected = true;
                StatusMessage = _languageManager.GetText("error_business_software");
                return;
            }

            IsExecuting = true;
            _currentCts = new CancellationTokenSource();

            try
            {
                ExecuteSingleJob(job, _currentCts.Token);
            }
            catch (OperationCanceledException)
            {
                // #14 - Job was stopped by business software detection
                StatusMessage = _languageManager.GetText("job_stopped_business_software");

                // #15 - Log the stop event
                LogBusinessSoftwareStop(job.Name);
            }
            catch (Exception ex)
            {
                StatusMessage = $"{_languageManager.GetText("error_execution")}: {ex.Message}";
            }
            finally
            {
                IsExecuting = false;
                _currentCts?.Dispose();
                _currentCts = null;
            }
        }

        public void ExecuteAllJobs()
        {
            // #13 - Block if business software detected
            if (_monitor.CheckNow())
            {
                IsBusinessSoftwareDetected = true;
                StatusMessage = _languageManager.GetText("error_business_software");
                return;
            }

            IsExecuting = true;
            _currentCts = new CancellationTokenSource();

            try
            {
                foreach (var job in _jobs.ToList())
                {
                    // #13 - Re-check before each job
                    if (_monitor.CheckNow())
                    {
                        StatusMessage = _languageManager.GetText("error_business_software");
                        LogBusinessSoftwareStop(job.Name);
                        break;
                    }

                    ExecuteSingleJob(job, _currentCts.Token);
                }

                if (!_currentCts.IsCancellationRequested)
                    StatusMessage = _languageManager.GetText("job_executed");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = _languageManager.GetText("job_stopped_business_software");
            }
            catch (Exception ex)
            {
                StatusMessage = $"{_languageManager.GetText("error_execution")}: {ex.Message}";
            }
            finally
            {
                IsExecuting = false;
                _currentCts?.Dispose();
                _currentCts = null;
            }
        }

        private void ExecuteSingleJob(BackupJob job, CancellationToken token)
        {
            IBackupStrategy strategy = job.Type == BackupType.Full
                ? new FullBackupStrategy()
                : new DifferentialBackupStrategy();

            var logService = LogService.Instance;
            var execution = new ServiceBackupExecution(strategy, logService, _stateService);

            execution.Execute(job, token);
        }

        // ===== BUSINESS SOFTWARE DETECTION HANDLER =====

        private void OnBusinessSoftwareDetectionChanged(bool detected)
        {
            IsBusinessSoftwareDetected = detected;

            if (detected)
            {
                StatusMessage = _languageManager.GetText("error_business_software");

                // #14 - If executing, cancel after current file
                _currentCts?.Cancel();
            }
            else
            {
                StatusMessage = _languageManager.GetText("business_software_cleared");
            }
        }

        // ===== LOGGING (#15) =====

        private void LogBusinessSoftwareStop(string jobName)
        {
            var logService = LogService.Instance;
            logService.Write(new ModelLogEntry
            {
                Timestamp = DateTime.Now,
                JobName = jobName,
                EventType = "Business Software Detected",
                EventDetails = $"Process: {_businessSoftwareName}",
                SourcePath = string.Empty,
                TargetPath = string.Empty,
                FileSize = 0,
                TransferTimeMs = 0
            });
            logService.Flush();
        }

        // ===== PERSISTENCE (#7 - same config.json, optimized for many jobs) =====

        private void LoadJobs()
        {
            if (!File.Exists(_configPath))
            {
                _jobs = new List<BackupJob>();
                return;
            }

            try
            {
                string json = File.ReadAllText(_configPath);
                var jobDtos = JsonSerializer.Deserialize<List<BackupJobDto>>(json);

                _jobs = jobDtos?.Select(dto => new BackupJob(
                    dto.Name ?? string.Empty,
                    dto.SourceDirectory ?? string.Empty,
                    dto.TargetDirectory ?? string.Empty,
                    dto.Type
                )).ToList() ?? new List<BackupJob>();
            }
            catch
            {
                _jobs = new List<BackupJob>();
            }
        }

        private void SaveJobs()
        {
            var jobDtos = _jobs.Select(j => new BackupJobDto
            {
                Name = j.Name,
                SourceDirectory = j.SourceDirectory,
                TargetDirectory = j.TargetDirectory,
                Type = j.Type
            }).ToList();

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(jobDtos, options);
            File.WriteAllText(_configPath, json);
        }

        // ===== SETTINGS PERSISTENCE (business software name) =====

        private string SettingsPath
        {
            get
            {
                string dir = Path.GetDirectoryName(_configPath)!;
                return Path.Combine(dir, "settings.json");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        _businessSoftwareName = settings.BusinessSoftwareName ?? "CalculatorApp";
                    }
                }
            }
            catch { /* use defaults */ }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings { BusinessSoftwareName = _businessSoftwareName };
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsPath, json);
            }
            catch { /* silent fail */ }
        }

        // ===== HELPERS =====

        private void RefreshObservableJobs()
        {
            Jobs.Clear();
            foreach (var job in _jobs)
                Jobs.Add(job);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ===== CLEANUP =====
        public void Dispose()
        {
            _monitor.DetectionChanged -= OnBusinessSoftwareDetectionChanged;
            _monitor.Stop();
            _currentCts?.Dispose();
        }

        // ===== DTO =====
        private class BackupJobDto
        {
            public string? Name { get; set; }
            public string? SourceDirectory { get; set; }
            public string? TargetDirectory { get; set; }
            public BackupType Type { get; set; }
        }

        private class AppSettings
        {
            public string? BusinessSoftwareName { get; set; }
        }
    }
}
