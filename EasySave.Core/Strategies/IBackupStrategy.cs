using EasySave.Core.Models;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    // ===== BACKUP STRATEGY INTERFACE =====
    public interface IBackupStrategy
    {
        // ===== EXECUTION METHODS =====

        Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted);

        Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted, JobExecutionContext context);
    }
}