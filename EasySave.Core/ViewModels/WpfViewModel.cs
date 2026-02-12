using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    //   - No MaxJobs limit 
    //   - Business software detection 
    //   - Observable collections for UI reactivity
    public class WpfViewModel : INotifyPropertyChanged, IDisposable
    {
        // ===== PRIVATE MEMBERS =====
        private List<BackupJob> _jobs;
        private readonly string _configPath;
        private readonly LanguageManager _languageManager;
        private readonly IStateService _stateService;
        private readonly BusinessSoftwareMonitor _monitor;
        private CancellationTokenSource? _currentCts;

        private string _encryptionKey = "Prosoft123";
        private List<string> _encryptionExtensions = new List<string> { ".txt", ".md", ".pdf" };
        private IEncryptionService _encryptionService = null!;

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
                SaveConfig();
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

            // --- Config path setup (SAME path as MainViewModel) ---
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "EasySave");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");

            // --- Load config (SAME format as MainViewModel) ---
            LoadConfig();
            UpdateEncryptionService();
            RefreshObservableJobs();

            // --- Business software monitor  ---
            _monitor = BusinessSoftwareMonitor.Instance;
            _monitor.ProcessName = _businessSoftwareName;
            _monitor.DetectionChanged += OnBusinessSoftwareDetectionChanged;
            _monitor.Start(2000);
        }

        // ===== LANGUAGE =====
        public LanguageManager GetLanguageManager() => _languageManager;

        public void SetLanguage(EasySave.Core.Models.Enums.Language lang)
        {
            _languageManager.SetLanguage(lang);
            OnPropertyChanged(nameof(Jobs));
        }

        // ===== JOB MANAGEMENT =====

        public int GetJobCount() => _jobs.Count;

        public bool CreateJob(string name, string source, string target, int typeInput)
        {
            //  NO MaxJobs validation

            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (_jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (!FileUtils.DirectoryExists(source))
                return false;

            BackupType type = typeInput == 2 ? BackupType.Differential : BackupType.Full;

            var job = new BackupJob(name, source, target, type);
            _jobs.Add(job);
            SaveConfig();
            RefreshObservableJobs();

            StatusMessage = _languageManager.GetText("job_created");
            return true;
        }

        public bool DeleteJob(BackupJob job)
        {
            if (job == null) return false;

            _jobs.Remove(job);
            SaveConfig();
            RefreshObservableJobs();

            StatusMessage = _languageManager.GetText("job_deleted");
            return true;
        }

        public bool DeleteJobByIndex(int zeroBasedIndex)
        {
            if (zeroBasedIndex < 0 || zeroBasedIndex >= _jobs.Count)
                return false;

            _jobs.RemoveAt(zeroBasedIndex);
            SaveConfig();
            RefreshObservableJobs();

            StatusMessage = _languageManager.GetText("job_deleted");
            return true;
        }

        // ===== EXECUTION WITH BUSINESS SOFTWARE CHECK  =====

        public async Task ExecuteJob(BackupJob job)
        {
            if (job == null) return;

            //  Block if business software detected
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
                await ExecuteSingleJob(job, _currentCts.Token);
                StatusMessage = _languageManager.GetText("job_executed");
            }
            catch (OperationCanceledException)
            {
                //  Job was stopped by business software detection
                StatusMessage = _languageManager.GetText("job_stopped_business_software");
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

        public async Task ExecuteAllJobs()
        {
            // Block if business software detected
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
                    // Re-check before each job
                    if (_monitor.CheckNow())
                    {
                        StatusMessage = _languageManager.GetText("error_business_software");
                        LogBusinessSoftwareStop(job.Name);
                        break;
                    }

                    await ExecuteSingleJob(job, _currentCts.Token);
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

        // ===== SINGLE JOB EXECUTION =====
        private async Task ExecuteSingleJob(BackupJob job, CancellationToken token)
        {
            // --- Select strategy based on job type ---
            IBackupStrategy strategy = job.Type == BackupType.Full
                ? new FullBackupStrategy()
                : new DifferentialBackupStrategy();

            // --- Create execution service ---
            var logService = LogService.Instance;
            var execution = new ServiceBackupExecution(strategy, logService, _stateService, _encryptionService);

            // --- Execute with business software name and cancellation token ---
            await execution.Execute(job, _businessSoftwareName, token);
        }

        // ===== ENCRYPTION =====
        private void UpdateEncryptionService()
        {
            _encryptionService = new EncryptionService(
                exePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe"),
                key: _encryptionKey,
                extensions: _encryptionExtensions
            );
        }

        // ===== BUSINESS SOFTWARE DETECTION HANDLER =====

        private void OnBusinessSoftwareDetectionChanged(bool detected)
        {
            IsBusinessSoftwareDetected = detected;

            if (detected)
            {
                StatusMessage = _languageManager.GetText("error_business_software");
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
                TransferTimeMs = 0,
                EncryptionTimeMs = 0
            });
            logService.Flush();
        }

        // ===== PERSISTENCE =====

        private void LoadConfig()
        {
            if (!File.Exists(_configPath))
            {
                _jobs = new List<BackupJob>();
                return;
            }

            try
            {
                string json = File.ReadAllText(_configPath);
                var configDto = JsonSerializer.Deserialize<AppConfigDto>(json);

                if (configDto != null)
                {
                    _encryptionKey = configDto.EncryptionKey ?? "Prosoft123";
                    _encryptionExtensions = configDto.EncryptionExtensions ?? new List<string> { ".txt", ".md", ".pdf" };
                    _businessSoftwareName = configDto.BusinessSoftwareName ?? "CalculatorApp";

                    _jobs = configDto.Jobs?.Select(dto => new BackupJob(
                        dto.Name ?? string.Empty,
                        dto.SourceDirectory ?? string.Empty,
                        dto.TargetDirectory ?? string.Empty,
                        dto.Type
                    )).ToList() ?? new List<BackupJob>();
                }
            }
            catch
            {
                _jobs = new List<BackupJob>();
            }
        }

        private void SaveConfig()
        {
            var configDto = new AppConfigDto
            {
                EncryptionKey = _encryptionKey,
                EncryptionExtensions = _encryptionExtensions,
                BusinessSoftwareName = _businessSoftwareName,
                Jobs = _jobs.Select(j => new BackupJobDto
                {
                    Name = j.Name,
                    SourceDirectory = j.SourceDirectory,
                    TargetDirectory = j.TargetDirectory,
                    Type = j.Type
                }).ToList()
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(configDto, options);
            File.WriteAllText(_configPath, json);
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
        private class AppConfigDto
        {
            public string? EncryptionKey { get; set; }
            public List<string>? EncryptionExtensions { get; set; }
            public string? BusinessSoftwareName { get; set; }
            public List<BackupJobDto>? Jobs { get; set; }
        }

        private class BackupJobDto
        {
            public string? Name { get; set; }
            public string? SourceDirectory { get; set; }
            public string? TargetDirectory { get; set; }
            public BackupType Type { get; set; }
        }
    }
}