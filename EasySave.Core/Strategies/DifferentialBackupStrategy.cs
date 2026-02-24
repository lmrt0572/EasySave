using EasySave.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasySave.Core.Strategies
{
    public class DifferentialBackupStrategy : BaseBackupStrategy
    {
        protected override List<string> GetFilesToProcess(BackupJob job)
            => Directory.EnumerateFiles(job.SourceDirectory, "*", SearchOption.AllDirectories)
                .Where(src =>
                {
                    var target = Path.Combine(job.TargetDirectory,
                        Path.GetRelativePath(job.SourceDirectory, src));
                    return !File.Exists(target) ||
                           File.GetLastWriteTime(src) > File.GetLastWriteTime(target);
                })
                .ToList();
    }
}