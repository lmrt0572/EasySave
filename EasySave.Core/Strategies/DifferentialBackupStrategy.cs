using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.Core.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    public class DifferentialBackupStrategy : IBackupStrategy
    {
        public async Task Execute(BackupJob job, IEncryptionService encryptionService,
            Action<string, string, long, long, int> onFileCompleted,
            JobExecutionContext context)
        {
            if (!FileUtils.DirectoryExists(job.SourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            var files = FileUtils.GetAllFilesRecursive(job.SourceDirectory);
            int thresholdKo = context.LargeFileThresholdKo;

            foreach (var sourceFile in files)
            {
                context.ThrowIfStoppedOrWaitIfPaused();

                var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
                var targetFile = Path.Combine(job.TargetDirectory, relativePath);

                if (File.Exists(targetFile) &&
                    File.GetLastWriteTime(sourceFile) <= File.GetLastWriteTime(targetFile))
                    continue;

                long fileSize = FileUtils.GetFileSize(sourceFile);

                await LargeFileTransferCoordinator.Instance.AcquireSlotIfLargeAsync(
                    fileSize, thresholdKo, context.Token);

                var stopwatch = Stopwatch.StartNew();
                int cryptTime = 0;

                try
                {
                    await FileUtils.CopyFileAsync(sourceFile, targetFile, context.Token);
                    stopwatch.Stop();

                    if (encryptionService.IsExtensionTargeted(targetFile))
                        cryptTime = await encryptionService.EncryptAsync(targetFile);

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