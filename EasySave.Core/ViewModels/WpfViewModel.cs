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
        private int _executionId;

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

        private string _logFormat = "json";

        // ===== NOTIFICATION STATE =====
        private string _notificationMessage = string.Empty;
        private string _notificationType = "info";
        private bool _isNotificationVisible;
        private int _notificationCounter;

        // ===== OBSERVABLE COLLECTION =====
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

        // ===== LOG FORMAT (synced with EasyLog.LogService) =====
        public string LogFormat
        {
            get => _logFormat;
            set
            {
                string normalized = NormalizeLogFormatString(value);
                if (_logFormat == normalized) return;
                _logFormat = normalized;
                LogService.Instance.SetLogFormat(ToLogFormat(_logFormat));
                SaveConfig();
                OnPropertyChanged();
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

        // ===== NOTIFICATION PROPERTIES =====

        public string NotificationMessage
        {
            get => _notificationMessage;
            private set { _notificationMessage = value; OnPropertyChanged(); }
        }

        public string NotificationType
        {
            get => _notificationType;
            private set { _notificationType = value; OnPropertyChanged(); }
        }

        public bool IsNotificationVisible
        {
            get => _isNotificationVisible;
            private set { _isNotificationVisible = value; OnPropertyChanged(); }
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

        // ===== LANGUAGE =====
        public LanguageManager GetLanguageManager() => _languageManager;

        public void SetLanguage(Language lang)
        {
            _languageManager.SetLanguage(lang);
            OnPropertyChanged(nameof(Jobs));
        }

        // ===== NOTIFICATION SYSTEM =====
        public async void ShowNotification(string message, string type = "info")
        {
            var myId = Interlocked.Increment(ref _notificationCounter);
            NotificationMessage = message;
            NotificationType = type;
            IsNotificationVisible = true;
            await Task.Delay(3500);
            if (_notificationCounter == myId)
                IsNotificationVisible = false;
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

        // ===== EXECUTION =====

        public async Task ExecuteJob(BackupJob job)
        {
            if (job == null) return;
            if (_monitor.CheckNow())
            {
                IsBusinessSoftwareDetected = true;
                StatusMessage = _languageManager.GetText("error_business_software");
                ShowNotification(_languageManager.GetText("error_business_software"), "error");
                return;
            }

            var myId = ++_executionId;
            var myCts = new CancellationTokenSource();
            _currentCts = myCts;
            IsExecuting = true;
            ResetProgress();

            try
            {
                await ExecuteSingleJob(job, myCts.Token);
                StatusMessage = _languageManager.GetText("job_executed");
                ShowNotification(_languageManager.GetText("notif_execution_complete"), "success");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = _languageManager.GetText("job_stopped_business_software");
                ShowNotification(_languageManager.GetText("job_stopped_business_software"), "warning");
            }
            catch (Exception ex)
            {
                StatusMessage = $"{_languageManager.GetText("error_execution")}: {ex.Message}";
                ShowNotification($"{_languageManager.GetText("notif_execution_error")}: {ex.Message}", "error");
            }
            finally
            {
                // ===== GUARD: only reset IsExecuting if this execution is still the current one =====
                if (_executionId == myId) IsExecuting = false;
                myCts.Dispose();
                if (_currentCts == myCts) _currentCts = null;
            }
        }

        public async Task ExecuteAllJobs()
        {
            if (_jobs.Count == 0) return;
            if (_monitor.CheckNow())
            {
                IsBusinessSoftwareDetected = true;
                StatusMessage = _languageManager.GetText("error_business_software");
                ShowNotification(_languageManager.GetText("error_business_software"), "error");
                return;
            }

            var myId = ++_executionId;
            var myCts = new CancellationTokenSource();
            _currentCts = myCts;
            IsExecuting = true;
            ResetProgress();

            try
            {
                foreach (var job in _jobs.ToList())
                {
                    if (_monitor.CheckNow())
                    {
                        StatusMessage = _languageManager.GetText("error_business_software");
                        ShowNotification(_languageManager.GetText("error_business_software"), "warning");
                        break;
                    }
                    await ExecuteSingleJob(job, myCts.Token);
                }
                if (!myCts.IsCancellationRequested)
                {
                    StatusMessage = _languageManager.GetText("job_executed");
                    ShowNotification(_languageManager.GetText("notif_execution_complete"), "success");
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = _languageManager.GetText("job_stopped_business_software");
                ShowNotification(_languageManager.GetText("job_stopped_business_software"), "warning");
            }
            catch (Exception ex)
            {
                StatusMessage = $"{_languageManager.GetText("error_execution")}: {ex.Message}";
                ShowNotification($"{_languageManager.GetText("notif_execution_error")}: {ex.Message}", "error");
            }
            finally
            {
                // ===== GUARD: only reset IsExecuting if this execution is still the current one =====
                if (_executionId == myId) IsExecuting = false;
                myCts.Dispose();
                if (_currentCts == myCts) _currentCts = null;
            }
        }

        private async Task ExecuteSingleJob(BackupJob job, CancellationToken token)
        {
            IBackupStrategy strategy = job.Type == BackupType.Full
                ? new FullBackupStrategy()
                : new DifferentialBackupStrategy();

            var logService = LogService.Instance;
            var execution = new ServiceBackupExecution(strategy, logService, _stateService, _encryptionService);

            CurrentJobName = job.Name;
            execution.StateUpdated += OnStateUpdated;

            try
            {
                await execution.Execute(job, _businessSoftwareName, token);
            }
            finally
            {
                execution.StateUpdated -= OnStateUpdated;
            }
        }

        // ===== PROGRESS =====

        private void OnStateUpdated(BackupJobState state)
        {
            ProgressPercent = state.Progression;
            int done = state.TotalFilesToCopy - state.RemainingFiles;
            ProgressText = $"{done} / {state.TotalFilesToCopy}";

            string file = state.CurrentSourceFile ?? string.Empty;
            if (file.Length > 0)
            {
                CurrentFileName = Path.GetFileName(file);
            }
            else if (state.Status == BackupStatus.Completed)
            {
                CurrentFileName = string.Empty;
                ProgressText = $"{state.TotalFilesToCopy} / {state.TotalFilesToCopy}";
                ProgressPercent = 100;
            }
        }

        private void ResetProgress()
        {
            CurrentJobName = string.Empty;
            ProgressPercent = 0;
            ProgressText = string.Empty;
            CurrentFileName = string.Empty;
        }

        // ===== ENCRYPTION =====
        private void UpdateEncryptionService()
        {
            _encryptionService = new EncryptionService(
                exePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe"),
                key: _encryptionKey,
                extensions: _encryptionExtensions);
        }

        // ===== BUSINESS SOFTWARE =====
        private void OnBusinessSoftwareDetectionChanged(bool detected)
        {
            IsBusinessSoftwareDetected = detected;
            if (detected)
            {
                StatusMessage = _languageManager.GetText("error_business_software");
                _currentCts?.Cancel();
                // ===== IMMEDIATE RESET: orphan the running execution via generation counter =====
                if (_isExecuting)
                {
                    _executionId++;
                    IsExecuting = false;
                    ResetProgress();
                }
            }
            else
            {
                StatusMessage = _languageManager.GetText("business_software_cleared");
            }
        }

        // ===== PERSISTENCE =====
        private void LoadConfig()
        {
            if (!File.Exists(_configPath))
            {
                _jobs = new List<BackupJob>();
                SyncLogServiceFormat();
                return;
            }
            try
            {
                var configDto = JsonSerializer.Deserialize<AppConfigDto>(File.ReadAllText(_configPath));
                if (configDto != null)
                {
                    _encryptionKey = configDto.EncryptionKey ?? "Prosoft123";
                    _encryptionExtensions = configDto.EncryptionExtensions ?? new List<string> { ".txt", ".md", ".pdf" };
                    _businessSoftwareName = configDto.BusinessSoftwareName ?? "CalculatorApp";
                    _logFormat = NormalizeLogFormatString(configDto.LogFormat ?? "json");
                    _jobs = configDto.Jobs?.Select(dto => new BackupJob(
                        dto.Name ?? "", dto.SourceDirectory ?? "", dto.TargetDirectory ?? "", dto.Type
                    )).ToList() ?? new List<BackupJob>();
                }
                SyncLogServiceFormat();
            }
            catch
            {
                _jobs = new List<BackupJob>();
                SyncLogServiceFormat();
            }
        }

        private void SyncLogServiceFormat()
        {
            LogService.Instance.SetLogFormat(ToLogFormat(_logFormat));
        }

        private static string NormalizeLogFormatString(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "json";
            var v = value.Trim().ToLowerInvariant();
            if (v == "xml" || v == "1") return "xml"; // "1" = Xml (enum value)
            return "json"; // json, 0, or unknown
        }

        private static EasyLog.Models.LogFormat ToLogFormat(string normalized)
        {
            return normalized == "xml" ? EasyLog.Models.LogFormat.Xml : EasyLog.Models.LogFormat.Json;
        }

        private void SaveConfig()
        {
            var configDto = new AppConfigDto
            {
                EncryptionKey = _encryptionKey,
                EncryptionExtensions = _encryptionExtensions,
                BusinessSoftwareName = _businessSoftwareName,
                LogFormat = _logFormat,
                Jobs = _jobs.Select(j => new BackupJobDto
                {
                    Name = j.Name,
                    SourceDirectory = j.SourceDirectory,
                    TargetDirectory = j.TargetDirectory,
                    Type = j.Type
                }).ToList()
            };
            File.WriteAllText(_configPath, JsonSerializer.Serialize(configDto, new JsonSerializerOptions { WriteIndented = true }));
        }

        // ===== HELPERS =====
        private void RefreshObservableJobs()
        {
            Jobs.Clear();
            foreach (var j in _jobs) Jobs.Add(j);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            _monitor.DetectionChanged -= OnBusinessSoftwareDetectionChanged;
            _monitor.Stop();
            _currentCts?.Dispose();
        }

        // ===== CONFIG DTO =====
        private class AppConfigDto
        {
            public string? EncryptionKey { get; set; }
            public List<string>? EncryptionExtensions { get; set; }
            public string? BusinessSoftwareName { get; set; }
            public string? LogFormat { get; set; }
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
