using EasySave.Core.Models;
using EasySave.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace EasySave.Core.Strategies
{
    // ===== DIFFERENTIAL BACKUP STRATEGY =====
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        // ===== EXECUTION =====
        public void Execute(BackupJob job, Action<string, string, long, long> onFileCompleted)
        {
            if (!FileUtils.DirectoryExists(job.SourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            var files = FileUtils.GetAllFilesRecursive(job.SourceDirectory);

            foreach (var sourceFile in files)
            {
                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);

                if (File.Exists(targetFile) && File.GetLastWriteTime(sourceFile) <= File.GetLastWriteTime(targetFile))
                {
                    continue;
                }

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    FileUtils.CopyFile(sourceFile, targetFile);
                    stopwatch.Stop();

                    long size = FileUtils.GetFileSize(sourceFile);

                    onFileCompleted(sourceFile, targetFile, size, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception)
                {
                    stopwatch.Stop();

                    onFileCompleted(sourceFile, targetFile, 0, -1);

                    throw;
                }
            }
        }
    }
}