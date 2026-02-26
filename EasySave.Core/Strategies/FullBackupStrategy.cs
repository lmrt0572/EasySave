using EasySave.Core.Models;
using EasySave.Core.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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