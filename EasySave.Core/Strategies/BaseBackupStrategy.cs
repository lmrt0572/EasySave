using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.Core.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    public abstract class BaseBackupStrategy : IBackupStrategy
    {
        // Implémentation de IBackupStrategy — context toujours non-null en V3
        public async Task Execute(BackupJob job, IEncryptionService encryptionService,
            Action<string, string, long, long, int> onFileCompleted,
            JobExecutionContext context,
            Action<string, string>? onFileStarted = null)
        {
            if (!FileUtils.DirectoryExists(job.SourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            var filesToCopy = GetFilesToProcess(job);
            int thresholdKo = context.LargeFileThresholdKo;
            var priorityExtensions = context.PriorityExtensions;
            bool usePriorityRule = priorityExtensions != null && priorityExtensions.Count > 0;

            if (!usePriorityRule)
            {
                foreach (var file in filesToCopy)
                {
                    context.ThrowIfStoppedOrWaitIfPaused();
                    await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
                }
                return;
            }

            // ===== PRIORITY: each job processes its priority files first, then non-priority (no cross-job wait) =====
            var deferredFiles = new List<string>();

            foreach (var file in filesToCopy)
            {
                context.ThrowIfStoppedOrWaitIfPaused();

                if (IsPriorityFile(file, priorityExtensions))
                    await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
                else
                    deferredFiles.Add(file);
            }

            foreach (var file in deferredFiles)
            {
                context.ThrowIfStoppedOrWaitIfPaused();
                await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
            }
        }

        protected abstract List<string> GetFilesToProcess(BackupJob job);

        protected static bool IsPriorityFile(string filePath, IReadOnlyList<string>? extensions)
        {
            if (extensions == null || extensions.Count == 0) return false;
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return extensions.Any(e => ext == (e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()));
        }

        protected static async Task ProcessOneFileAsync(BackupJob job, string sourceFile, int thresholdKo,
            IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted,
            JobExecutionContext context, Action<string, string>? onFileStarted = null)
        {
            var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
            var targetFile = Path.Combine(job.TargetDirectory, relativePath);

            onFileStarted?.Invoke(sourceFile, targetFile);

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
            catch (OperationCanceledException) { throw; }
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