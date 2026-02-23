using EasySave.Core.Models;
using EasySave.Core.Utils;
using EasySave.Core.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    // ===== DIFFERENTIAL BACKUP STRATEGY =====
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        // ===== EXECUTION (Console compatibility) =====
        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted)
        {
            await Execute(job, encryptionService, onFileCompleted, context: null!);
        }

        // ===== EXECUTION (with pause/stop support + large file coordination) =====
        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted, JobExecutionContext context)
        {
            if (!FileUtils.DirectoryExists(job.SourceDirectory))
            {
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");
            }

            var files = FileUtils.GetAllFilesRecursive(job.SourceDirectory);
            int thresholdKo = context?.LargeFileThresholdKo ?? 0;

            foreach (var sourceFile in files)
            {
                // ===== BUSINESS SOFTWARE / PAUSE / STOP =====
                context?.ThrowIfStoppedOrWaitIfPaused();

                // ===== PRIORITY FILES ===== (placeholder - to be implemented)

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);

                if (File.Exists(targetFile) && File.GetLastWriteTime(sourceFile) <= File.GetLastWriteTime(targetFile))
                {
                    continue;
                }

                long fileSize = FileUtils.GetFileSize(sourceFile);

                // ===== LARGE FILES =====
                await LargeFileTransferCoordinator.Instance.AcquireSlotIfLargeAsync(fileSize, thresholdKo, context?.Token ?? CancellationToken.None);

                var stopwatch = Stopwatch.StartNew();
                int cryptTime = 0;

                try
                {
                    if (context != null)
                        await FileUtils.CopyFileAsync(sourceFile, targetFile, context.Token);
                    else
                        FileUtils.CopyFile(sourceFile, targetFile);
                    stopwatch.Stop();

                    if (encryptionService.IsExtensionTargeted(targetFile))
                    {
                        cryptTime = await encryptionService.EncryptAsync(targetFile);
                    }

                    onFileCompleted(sourceFile, targetFile, fileSize, stopwatch.ElapsedMilliseconds, cryptTime);
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
                finally
                {
                    LargeFileTransferCoordinator.Instance.ReleaseSlotIfLarge(fileSize, thresholdKo);
                }
            }
        }
    }
}