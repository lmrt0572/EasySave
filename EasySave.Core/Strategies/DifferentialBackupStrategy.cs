using EasySave.Core.Models;
using EasySave.Core.Utils;
using EasySave.Core.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    // ===== DIFFERENTIAL BACKUP STRATEGY =====
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        // ===== V2 EXECUTION (Console compatibility) =====
        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted)
        {
            await Execute(job, encryptionService, onFileCompleted, context: null!);
        }

        // ===== V3 EXECUTION (with pause/stop support) =====
        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted, JobExecutionContext context)
        {
            if (!FileUtils.DirectoryExists(job.SourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            var files = FileUtils.GetAllFilesRecursive(job.SourceDirectory);

            foreach (var sourceFile in files)
            {

                context?.ThrowIfStoppedOrWaitIfPaused();

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);

                if (File.Exists(targetFile) && File.GetLastWriteTime(sourceFile) <= File.GetLastWriteTime(targetFile))
                {
                    continue;
                }

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
                catch (OperationCanceledException)
                {
                    throw; 
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