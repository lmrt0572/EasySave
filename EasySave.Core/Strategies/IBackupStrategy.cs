using EasySave.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Strategies
{
    // ===== BACKUP STRATEGY INTERFACE =====
    public interface IBackupStrategy
    {
        Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted);
    }
}
