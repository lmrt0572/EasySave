using EasySave.Core.Models;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Strategies
{
    // ===== BACKUP STRATEGY INTERFACE =====
    public interface IBackupStrategy
    {
        // V2  (kept for Console compatibility)
        Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted);

        // V3  with pause/stop support
        Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted, JobExecutionContext context);
    }
}