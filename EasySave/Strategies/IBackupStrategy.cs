using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Strategies
{
    public interface IBackupStrategy
    {
        void Execute(BackupJob job, Action<string, string, long, long> onFileCompleted);
    }
}
