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
        private AppConfig _config;
        private readonly string _configPath;
        private readonly LanguageManager _languageManager;
        private readonly IStateService _stateService;
        private readonly BusinessSoftwareMonitor _monitor;
        private CancellationTokenSource? _currentCts;
        private int _executionId;
        private IEncryptionService _encryptionService = null!;

        private bool _isBusinessSoftwareDetected;
        private bool _isExecuting;
        private string _statusMessage = string.Empty;
        private string _currentJobName = string.Empty;
        private int _progressPercent;
        private string _progressText = string.Empty;
        private string _currentFileName = string.Empty;

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
            get => _config.BusinessSoftwareName;
            set { _config.BusinessSoftwareName = value; _monitor.ProcessName = value; SaveConfig(); OnPropertyChanged(); }
        }

        public string EncryptionKey
        {
            get => _config.EncryptionKey;
            set { _config.EncryptionKey = value; UpdateEncryptionService(); SaveConfig(); OnPropertyChanged(); OnPropertyChanged(nameof(IsEncryptionActive)); }
        }

        public string EncryptionExtensionsText
        {
            get => string.Join(", ", _config.EncryptionExtensions);
            set
            {
                _config.EncryptionExtensions = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim().ToLowerInvariant()).Where(e => !string.IsNullOrEmpty(e))
                    .Select(e => e.StartsWith('.') ? e : "." + e).Distinct().ToList();
                UpdateEncryptionService(); SaveConfig(); OnPropertyChanged(); OnPropertyChanged(nameof(IsEncryptionActive));
            }
        }

        // ===== ENCRYPTION STATUS (for Dashboard) =====
        public bool IsEncryptionActive => !string.IsNullOrWhiteSpace(_config.EncryptionKey) && _config.EncryptionExtensions.Count > 0;

        // ===== LOG FORMAT =====
        public string LogFormat
        {
            get => _config.LogFormat == EasyLog.Models.LogFormat.Xml ? "xml" : "json";
            set
            {
                var f = ParseLogFormat(value);
                if (_config.LogFormat == f) return;
                _config.LogFormat = f;
                LogService.Instance.SetLogFormat(f);
                SaveConfig(); OnPropertyChanged();
            }
        }

        // ===== PROGRESS =====
        public string CurrentJobName { get => _currentJobName; private set { _currentJobName = value; OnPropertyChanged(); } }
        public int ProgressPercent { get => _progressPercent; private set { _progressPercent = value; OnPropertyChanged(); } }
        public string ProgressText { get => _progressText; private set { _progressText = value; OnPropertyChanged(); } }
        public string CurrentFileName { get => _currentFileName; private set { _currentFileName = value; OnPropertyChanged(); } }

        // ===== NOTIFICATION =====
        public string NotificationMessage { get => _notificationMessage; private set { _notificationMessage = value; OnPropertyChanged(); } }
        public string NotificationType { get => _notificationType; private set { _notificationType = value; OnPropertyChanged(); } }
        public bool IsNotificationVisible { get => _isNotificationVisible; private set { _isNotificationVisible = value; OnPropertyChanged(); } }

        // ===== CONSTRUCTOR =====
        public WpfViewModel(LanguageManager languageManager)
        {
            _languageManager = languageManager ?? throw new ArgumentNullException(nameof(languageManager));
            _stateService = new StateService();
            _config = new AppConfig();
            Jobs = new ObservableCollection<BackupJob>();

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appDataPath, "EasySave");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "config.json");

            LoadConfig();
            UpdateEncryptionService();
            RefreshObservableJobs();

            _monitor = BusinessSoftwareMonitor.Instance;
            _monitor.ProcessName = _config.BusinessSoftwareName;
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
            NotificationMessage = message; NotificationType = type; IsNotificationVisible = true;
            await Task.Delay(3500);
            if (_notificationCounter == myId) IsNotificationVisible = false;
        }

        // ===== JOB MANAGEMENT =====
        public int GetJobCount() => _config.Jobs.Count;

        public bool CreateJob(string name, string source, string target, int typeInput)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (_config.Jobs.Any(j => j.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) return false;
            if (!FileUtils.DirectoryExists(source)) return false;

            _config.Jobs.Add(new BackupJob(name, source, target, typeInput == 2 ? BackupType.Differential : BackupType.Full));
            SaveConfig(); RefreshObservableJobs();
            StatusMessage = _languageManager.GetText("job_created");
            return true;
        }

        public bool DeleteJob(BackupJob job)
        {
            if (job == null) return false;
            _config.Jobs.Remove(job);
            SaveConfig(); RefreshObservableJobs();
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
            IsExecuting = true; ResetProgress();

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
                if (_executionId == myId) IsExecuting = false;
                myCts.Dispose();
                if (_currentCts == myCts) _currentCts = null;
            }
        }

        public async Task ExecuteAllJobs()
        {
            if (_config.Jobs.Count == 0) return;
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
            IsExecuting = true; ResetProgress();

            try
            {
                foreach (var job in _config.Jobs.ToList())
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
                if (_executionId == myId) IsExecuting = false;
                myCts.Dispose();
                if (_currentCts == myCts) _currentCts = null;
            }
        }

        private async Task ExecuteSingleJob(BackupJob job, CancellationToken token)
        {
            IBackupStrategy strategy = job.Type == BackupType.Full ? new FullBackupStrategy() : new DifferentialBackupStrategy();
            var execution = new ServiceBackupExecution(strategy, LogService.Instance, _stateService, _encryptionService);

            CurrentJobName = job.Name;
            execution.StateUpdated += OnStateUpdated;
            try { await execution.Execute(job, _config.BusinessSoftwareName, token); }
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
        { CurrentJobName = string.Empty; ProgressPercent = 0; ProgressText = string.Empty; CurrentFileName = string.Empty; }

        // ===== ENCRYPTION =====
        private void UpdateEncryptionService()
        {
            _encryptionService = new EncryptionService(
                exePath: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CryptoSoft.exe"),
                key: _config.EncryptionKey,
                extensions: _config.EncryptionExtensions);
        }

        // ===== BUSINESS SOFTWARE =====
        private void OnBusinessSoftwareDetectionChanged(bool detected)
        {
            IsBusinessSoftwareDetected = detected;
            if (detected)
            {
                StatusMessage = _languageManager.GetText("error_business_software");
                _currentCts?.Cancel();
                if (_isExecuting) { _executionId++; IsExecuting = false; ResetProgress(); }
            }
            else StatusMessage = _languageManager.GetText("business_software_cleared");
        }

        // ===== PERSISTENCE =====
        private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

        private void LoadConfig()
        {
            if (!File.Exists(_configPath)) { _config = new AppConfig(); ApplyLogFormat(); return; }
            try
            {
                string json = File.ReadAllText(_configPath);
                if (string.IsNullOrWhiteSpace(json)) { _config = new AppConfig(); ApplyLogFormat(); return; }
                json = json.TrimStart();

                if (json.StartsWith("["))
                {
                    var jobs = JsonSerializer.Deserialize<List<BackupJob>>(json);
                    _config = new AppConfig { Jobs = jobs ?? new() };
                }
                else
                    _config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch { _config = new AppConfig(); }
            ApplyLogFormat();
        }

        private void ApplyLogFormat() => LogService.Instance.SetLogFormat(_config.LogFormat);

        private static EasyLog.Models.LogFormat ParseLogFormat(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return EasyLog.Models.LogFormat.Json;
            return value.Trim().ToLowerInvariant() is "xml" or "1" ? EasyLog.Models.LogFormat.Xml : EasyLog.Models.LogFormat.Json;
        }

        private void SaveConfig()
        {
            try { File.WriteAllText(_configPath, JsonSerializer.Serialize(_config, _jsonOpts)); }
            catch { }
        }

        // ===== HELPERS =====
        private void RefreshObservableJobs()
        { Jobs.Clear(); foreach (var j in _config.Jobs) Jobs.Add(j); }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Dispose()
        {
            _monitor.DetectionChanged -= OnBusinessSoftwareDetectionChanged;
            _monitor.Stop();
            _currentCts?.Dispose();
        }
    }
}
