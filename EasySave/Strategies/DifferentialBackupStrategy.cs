using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EasySave.Strategies
{
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        public void Execute(BackupJob job, Action<string, string, long, long> onFileCompleted)
        {
            if (!Directory.Exists(job.SourceDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"Source directory not found: {job.SourceDirectory}");
            }

            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);

            foreach (var sourceFile in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);

                var targetFile = Path.Combine(job.TargetDirectory, relativePath);

                if (File.Exists(targetFile) && File.GetLastWriteTime(sourceFile) <= File.GetLastWriteTime(targetFile))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    File.Copy(sourceFile, targetFile, true);
                    stopwatch.Stop();

                    var size = new FileInfo(sourceFile).Length;

                    onFileCompleted(sourceFile, targetFile, size, stopwatch.ElapsedMilliseconds);
                }

                catch
                {
                    stopwatch.Stop();

                    onFileCompleted(sourceFile, targetFile, 0, -1);

                    throw;
                }
            }
        }
    }

}
