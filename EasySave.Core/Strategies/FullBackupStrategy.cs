using EasySave.Core.Models;
using EasySave.Core.Utils;
using EasySave.Core.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    // ===== FULL BACKUP STRATEGY =====
    public class FullBackupStrategy : IBackupStrategy
    {
        // ===== EXECUTION =====
        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted)
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

                var stopwatch = Stopwatch.StartNew();
                int cryptTime = 0;

                try
                {
                    FileUtils.CopyFile(sourceFile, targetFile);
                    stopwatch.Stop();

                    if (encryptionService.IsExtensionTargeted(targetFile))
                    {
                        cryptTime = await encryptionService.EncryptAsync(targetFile);
                    }

                    long size = FileUtils.GetFileSize(sourceFile);
                    onFileCompleted(sourceFile, targetFile, size, stopwatch.ElapsedMilliseconds, cryptTime);
                }
                catch (Exception)
                {
                    if (stopwatch.IsRunning) stopwatch.Stop();
                    onFileCompleted(sourceFile, targetFile, 0, -1, -1);
                    throw;
                }
            }
        }
    }
}