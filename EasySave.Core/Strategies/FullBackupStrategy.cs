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
    // ===== FULL BACKUP STRATEGY =====
    public class FullBackupStrategy : IBackupStrategy
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
                // No non-priority file may transfer while priority extensions are pending on any job.

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);

                long fileSize = FileUtils.GetFileSize(sourceFile);

                // ===== LARGE FILES =====
                // If file size > threshold Ko: wait for the "large file" slot (only one at a time globally).
                // Small files can transfer in parallel.
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
                    // Release the large-file slot so other jobs can transfer large files
                    LargeFileTransferCoordinator.Instance.ReleaseSlotIfLarge(fileSize, thresholdKo);
                }
            }
        }
    }
}