using EasySave.Core.Models;
using EasySave.Core.Services;
using System;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    public interface IBackupStrategy
    {
        Task Execute(BackupJob job, IEncryptionService encryptionService,
            Action<string, string, long, long, int> onFileCompleted,
            JobExecutionContext context,
            Action<string, string>? onFileStarted = null);
    }
}