using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasySave.Core.Models.Enums;

namespace EasySave.Core.Models
{
    // ===== JOB PROGRESS INFO =====
    // Observable per-job progress for WPF binding
    // Each running job gets one instance, displayed in the UI ItemsControl
    public class JobProgressInfo : INotifyPropertyChanged
    {
        // ===== PRIVATE FIELDS =====
        private int _progression;
        private int _totalFiles;
        private int _remainingFiles;
        private string _currentFile = string.Empty;
        private BackupStatus _status = BackupStatus.Inactive;

        // ===== EVENTS =====
        public event PropertyChangedEventHandler? PropertyChanged;

        // ===== PROPERTIES =====

        public string JobName { get; }
        public int Progression
        {
            get => _progression;
            set { _progression = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilesDone)); }
        }
        public int TotalFiles
        {
            get => _totalFiles;
            set { _totalFiles = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilesDone)); }
        }

        public int RemainingFiles
        {
            get => _remainingFiles;
            set { _remainingFiles = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilesDone)); }
        }

        public int FilesDone => TotalFiles - RemainingFiles;

        public string CurrentFile
        {
            get => _currentFile;
            set { _currentFile = value ?? string.Empty; OnPropertyChanged(); }
        }
        public BackupStatus Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRunning)); OnPropertyChanged(nameof(IsPaused)); }
        }
        public bool IsRunning => _status == BackupStatus.Running || _status == BackupStatus.Active;

        public bool IsPaused => _status == BackupStatus.Paused;

        // ===== CONSTRUCTOR =====
        public JobProgressInfo(string jobName)
        {
            JobName = jobName;
        }

        // ===== INotifyPropertyChanged =====
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}