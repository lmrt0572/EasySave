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
    // ===== DIFFERENTIAL BACKUP STRATEGY =====
    public class DifferentialBackupStrategy : BaseBackupStrategy
    {
        // ===== FILE SELECTION =====
        protected override List<string> GetFilesToProcess(BackupJob job)
        {
            var allFiles = FileUtils.GetAllFilesRecursive(job.SourceDirectory);

            return allFiles.Where(sourceFile => {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);

                // Only process if target doesn't exist or source is newer
                return !File.Exists(targetFile) || File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(targetFile);
            }).ToList();
        }
    }
}