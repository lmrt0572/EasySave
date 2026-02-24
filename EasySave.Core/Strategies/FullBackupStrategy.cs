using EasySave.Core.Models;
using System.Collections.Generic;
using System.IO;

namespace EasySave.Core.Strategies
{
    public class FullBackupStrategy : BaseBackupStrategy
    {
        protected override List<string> GetFilesToProcess(BackupJob job)
            => new List<string>(Directory.EnumerateFiles(job.SourceDirectory, "*", SearchOption.AllDirectories));
    }
}