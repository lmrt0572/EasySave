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
        Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted);

        Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted, JobExecutionContext context, Action<string, string>? onFileStarted = null);
    }
}