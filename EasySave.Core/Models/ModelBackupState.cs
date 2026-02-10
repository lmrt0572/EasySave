using System;
using EasySave.Core.Models.Enums;

namespace EasySave.Core.Models
{
    // ===== BACKUP JOB STATE =====
    public class BackupJobState
    {
        // ===== REQUIRED FIELDS =====

        public string JobName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public BackupStatus Status { get; set; }

        // ===== ACTIVE JOB FIELDS =====
        public int TotalFilesToCopy { get; set; }
        public long TotalFilesSize { get; set; }
        public int Progression { get; set; }
        public int RemainingFiles { get; set; }
        public long RemainingFilesSize { get; set; }
        public string? CurrentSourceFile { get; set; }
        public string? CurrentDestinationFile { get; set; }

        // ===== CONSTRUCTOR =====

        public BackupJobState()
        {
            Timestamp = DateTime.Now;
            Status = BackupStatus.Inactive;
        }

        public BackupJobState(string jobName) : this()
        {
            JobName = jobName;
        }

        // ===== BUSINESS LOGIC METHODS =====
        public double GetProgression()
        {
            if (TotalFilesToCopy == 0)
                return 0;

            int filesCopied = TotalFilesToCopy - RemainingFiles;
            return (filesCopied * 100.0) / TotalFilesToCopy;
        }
        public void StartBackup(long totalFiles, long totalSize)
        {
            Status = BackupStatus.Active;
            TotalFilesToCopy = (int)totalFiles;
            TotalFilesSize = totalSize;
            RemainingFiles = (int)totalFiles;
            RemainingFilesSize = totalSize;
            Progression = 0;
            Timestamp = DateTime.Now;
        }

        public void UpdateCurrentFile(string sourceFile, string destinationFile)
        {
            CurrentSourceFile = sourceFile;
            CurrentDestinationFile = destinationFile;
            Timestamp = DateTime.Now;
        }

        public void CompleteFile(long fileSize)
        {
            RemainingFiles--;
            RemainingFilesSize -= fileSize;
            Progression = (int)GetProgression();
            Timestamp = DateTime.Now;

            CurrentSourceFile = null;
            CurrentDestinationFile = null;
        }

        public void Finish()
        {
            Status = BackupStatus.Completed;
            Progression = 100;
            RemainingFiles = 0;
            RemainingFilesSize = 0;
            CurrentSourceFile = null;
            CurrentDestinationFile = null;
            Timestamp = DateTime.Now;
        }

        public void SetError()
        {
            Status = BackupStatus.Error;
            Timestamp = DateTime.Now;
        }
    }
}
