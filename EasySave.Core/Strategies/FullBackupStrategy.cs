using EasySave.Core.Models;
using EasySave.Core.Utils;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    // ===== FULL BACKUP STRATEGY =====
    public class FullBackupStrategy : BaseBackupStrategy
    {
        // ===== FILE SELECTION =====
        protected override List<string> GetFilesToProcess(BackupJob job)
        {
            return FileUtils.GetAllFilesRecursive(job.SourceDirectory).ToList();
        }
    }
}