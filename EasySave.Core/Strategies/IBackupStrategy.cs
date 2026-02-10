using EasySave.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Strategies
{
    // ===== BACKUP STRATEGY INTERFACE =====
    public interface IBackupStrategy
    {
        void Execute(BackupJob job, Action<string, string, long, long> onFileCompleted);
    }
}
