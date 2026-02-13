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

        private string _currentJobName = string.Empty;
        private int _progressPercent;
        private string _progressText = string.Empty;
        private string _currentFileName = string.Empty;

        public ObservableCollection<BackupJob> Jobs { get; }
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
            set { _businessSoftwareName = value; _monitor.ProcessName = value; SaveConfig(); OnPropertyChanged(); }
        }

        public string EncryptionKey
        {
            get => _encryptionKey;
            set { _encryptionKey = value; UpdateEncryptionService(); SaveConfig(); OnPropertyChanged(); }
        }

        public string EncryptionExtensionsText
        {
            get => string.Join(", ", _encryptionExtensions);
            set
            {
                _encryptionExtensions = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e))
                    .Select(e => e.StartsWith('.') ? e : "." + e).ToList();
                UpdateEncryptionService(); SaveConfig(); OnPropertyChanged();
            }
        }

        // ===== PROGRESS PROPERTIES =====

        public string CurrentJobName
        {
            get => _currentJobName;
            private set { _currentJobName = value; OnPropertyChanged(); }
        }

        public int ProgressPercent
        {
            get => _progressPercent;
            private set { _progressPercent = value; OnPropertyChanged(); }
        }

        public string ProgressText
        {
            get => _progressText;
            private set { _progressText = value; OnPropertyChanged(); }
        }

        public string CurrentFileName
        {
            get => _currentFileName;
            private set { _currentFileName = value; OnPropertyChanged(); }
        }

        // ===== CONSTRUCTOR =====
        public WpfViewModel(LanguageManager languageManager)
        {
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _stateService = new StateService();
            _jobs = new List<BackupJob>();
            Jobs = new ObservableCollection<BackupJob>();

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "EasySave");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");

            LoadConfig();
            UpdateEncryptionService();
            RefreshObservableJobs();

            _monitor = BusinessSoftwareMonitor.Instance;
            _monitor.ProcessName = _businessSoftwareName;
            _monitor.DetectionChanged += OnBusinessSoftwareDetectionChanged;
            _monitor.Start(2000);
        }

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
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (_jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return false;
            if (!FileUtils.DirectoryExists(source)) return false;

            var job = new BackupJob(name, source, target, typeInput == 2 ? BackupType.Differential : BackupType.Full);
            _jobs.Add(job);
            SaveConfig(); RefreshObservableJobs();
            StatusMessage = _languageManager.GetText("job_created");
            return true;
        }

        public bool DeleteJob(BackupJob job)
        {
            if (job == null) return false;
            _jobs.Remove(job); SaveConfig(); RefreshObservableJobs();
            StatusMessage = _languageManager.GetText("job_deleted");
            return true;
        }

        public bool DeleteJobByIndex(int idx)
        {
            if (idx < 0 || idx >= _jobs.Count) return false;
            _jobs.RemoveAt(idx); SaveConfig(); RefreshObservableJobs();
            StatusMessage = _languageManager.GetText("job_deleted");
            return true;
        }

        // ===== EXECUTION =====

        public async Task ExecuteJob(BackupJob job)
        {
            if (job == null) return;
            if (_monitor.CheckNow()) { IsBusinessSoftwareDetected = true; StatusMessage = _languageManager.GetText("error_business_software"); return; }

            IsExecuting = true; _currentCts = new CancellationTokenSource(); ResetProgress();
            try
            {
                await ExecuteSingleJob(job, _currentCts.Token);
                StatusMessage = _languageManager.GetText("job_executed");
            }
            catch (OperationCanceledException) { StatusMessage = _languageManager.GetText("job_stopped_business_software"); }
            catch (Exception ex) { StatusMessage = $"{_languageManager.GetText("error_execution")}: {ex.Message}"; }
            finally { IsExecuting = false; _currentCts?.Dispose(); _currentCts = null; }
        }

        public async Task ExecuteAllJobs()
        {
            if (_monitor.CheckNow()) { IsBusinessSoftwareDetected = true; StatusMessage = _languageManager.GetText("error_business_software"); return; }

            IsExecuting = true; _currentCts = new CancellationTokenSource(); ResetProgress();
            try
            {
                foreach (var job in _jobs.ToList())
                {
                    if (_monitor.CheckNow()) { StatusMessage = _languageManager.GetText("error_business_software"); break; }
                    await ExecuteSingleJob(job, _currentCts.Token);
                }
                if (!_currentCts.IsCancellationRequested) StatusMessage = _languageManager.GetText("job_executed");
            }
            catch (OperationCanceledException) { StatusMessage = _languageManager.GetText("job_stopped_business_software"); }
            catch (Exception ex) { StatusMessage = $"{_languageManager.GetText("error_execution")}: {ex.Message}"; }
            finally { IsExecuting = false; _currentCts?.Dispose(); _currentCts = null; }
        }

        private async Task ExecuteSingleJob(BackupJob job, CancellationToken token)
        {
            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe");
            if (!File.Exists(exePath)) { StatusMessage = "ERROR: CryptoSoft.exe not found"; return; }

            IBackupStrategy strategy = job.Type == BackupType.Full ? new FullBackupStrategy() : new DifferentialBackupStrategy();
            var logService = LogService.Instance;
            var execution = new ServiceBackupExecution(strategy, logService, _stateService, _encryptionService);

            CurrentJobName = job.Name;
            execution.StateUpdated += OnStateUpdated;
            try { await execution.Execute(job, _businessSoftwareName, token); }
            finally { execution.StateUpdated -= OnStateUpdated; }
        }

        // ===== PROGRESS =====

        private void OnStateUpdated(BackupJobState state)
        {
            ProgressPercent = state.Progression;
            int done = state.TotalFilesToCopy - state.RemainingFiles;
            ProgressText = $"{done} / {state.TotalFilesToCopy}";

            string file = state.CurrentSourceFile ?? string.Empty;
            if (file.Length > 0) CurrentFileName = Path.GetFileName(file);
            else if (state.Status == BackupStatus.Completed)
            {
                CurrentFileName = string.Empty;
                ProgressText = $"{state.TotalFilesToCopy} / {state.TotalFilesToCopy}";
                ProgressPercent = 100;
            }
        }

        private void ResetProgress()
        {
            CurrentJobName = string.Empty; ProgressPercent = 0;
            ProgressText = string.Empty; CurrentFileName = string.Empty;
        }

        // ===== ENCRYPTION =====
        private void UpdateEncryptionService()
        {
            _encryptionService = new EncryptionService(
                exePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe"),
                key: _encryptionKey, extensions: _encryptionExtensions);
        }

        // ===== BUSINESS SOFTWARE =====
        private void OnBusinessSoftwareDetectionChanged(bool detected)
        {
            IsBusinessSoftwareDetected = detected;
            if (detected) { StatusMessage = _languageManager.GetText("error_business_software"); _currentCts?.Cancel(); }
            else StatusMessage = _languageManager.GetText("business_software_cleared");
        }

        // ===== PERSISTENCE =====
        private void LoadConfig()
        {
            if (!File.Exists(_configPath)) { _jobs = new List<BackupJob>(); return; }
            try
            {
                var configDto = JsonSerializer.Deserialize<AppConfigDto>(File.ReadAllText(_configPath));
                if (configDto != null)
                {
                    _encryptionKey = configDto.EncryptionKey ?? "Prosoft123";
                    _encryptionExtensions = configDto.EncryptionExtensions ?? new List<string> { ".txt", ".md", ".pdf" };
                    _businessSoftwareName = configDto.BusinessSoftwareName ?? "CalculatorApp";
                    _jobs = configDto.Jobs?.Select(dto => new BackupJob(dto.Name ?? "", dto.SourceDirectory ?? "", dto.TargetDirectory ?? "", dto.Type)).ToList() ?? new List<BackupJob>();
                }
            }
            catch { _jobs = new List<BackupJob>(); }
        }

        private void SaveConfig()
        {
            var configDto = new AppConfigDto
            {
                EncryptionKey = _encryptionKey,
                EncryptionExtensions = _encryptionExtensions,
                BusinessSoftwareName = _businessSoftwareName,
                Jobs = _jobs.Select(j => new BackupJobDto { Name = j.Name, SourceDirectory = j.SourceDirectory, TargetDirectory = j.TargetDirectory, Type = j.Type }).ToList()
            };
            File.WriteAllText(_configPath, JsonSerializer.Serialize(configDto, new JsonSerializerOptions { WriteIndented = true }));
        }

        private void RefreshObservableJobs() { Jobs.Clear(); foreach (var j in _jobs) Jobs.Add(j); }
        private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        public void Dispose() { _monitor.DetectionChanged -= OnBusinessSoftwareDetectionChanged; _monitor.Stop(); _currentCts?.Dispose(); }

        private class AppConfigDto { public string? EncryptionKey { get; set; } public List<string>? EncryptionExtensions { get; set; } public string? BusinessSoftwareName { get; set; } public List<BackupJobDto>? Jobs { get; set; } }
        private class BackupJobDto { public string? Name { get; set; } public string? SourceDirectory { get; set; } public string? TargetDirectory { get; set; } public BackupType Type { get; set; } }
    }
}
