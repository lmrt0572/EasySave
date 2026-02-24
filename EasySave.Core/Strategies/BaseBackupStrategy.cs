using EasySave.Core.Models;
using EasySave.Core.Utils;
using EasySave.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Strategies
{
    // ===== BASE BACKUP STRATEGY =====
    public abstract class BaseBackupStrategy : IBackupStrategy
    {
        // ===== EXECUTION =====
        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted)
        {
            await Execute(job, encryptionService, onFileCompleted, context: null!);
        }

        // ===== EXECUTION =====
        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted, JobExecutionContext context, Action<string, string>? onFileStarted = null)
        {
            // ===== SOURCE DIRECTORY CHECK =====
            if (!FileUtils.DirectoryExists(job.SourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            var filesToCopy = GetFilesToProcess(job);

            int thresholdKo = context?.LargeFileThresholdKo ?? 0;
            var priorityExtensions = context?.PriorityExtensions;
            bool usePriorityRule = context != null && priorityExtensions != null && priorityExtensions.Count > 0;

            if (!usePriorityRule)
            {
                foreach (var file in filesToCopy)
                {
                    // ===== PAUSE / STOP CHECK =====
                    context?.ThrowIfStoppedOrWaitIfPaused();
                    await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
                }
                return;
            }

            // ===== PRIORITY RULE COORDINATION =====
            PriorityFileCoordinator.Instance.RegisterJob(context!.JobName);
            var deferredFiles = new List<string>();

            try
            {
                foreach (var file in filesToCopy)
                {
                    // ===== PAUSE / STOP CHECK =====
                    context.ThrowIfStoppedOrWaitIfPaused();

                    // ===== PRIORITY CHECK =====
                    if (IsPriorityFile(file, priorityExtensions))
                        await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
                    else
                        deferredFiles.Add(file);
                }

                // ===== WAIT FOR NON-PRIORITY PHASE =====
                PriorityFileCoordinator.Instance.NotifyPriorityPhaseDone(context.JobName);
                await PriorityFileCoordinator.Instance.WaitUntilCanTransferNonPriorityAsync(context.Token);

                foreach (var file in deferredFiles)
                {
                    // ===== PAUSE / STOP CHECK =====
                    context.ThrowIfStoppedOrWaitIfPaused();
                    await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
                }
            }
            finally
            {
                PriorityFileCoordinator.Instance.UnregisterJob(context.JobName);
            }
        }

        // ===== ABSTRACT METHODS =====
        protected abstract List<string> GetFilesToProcess(BackupJob job);

        // ===== HELPERS =====
        protected static bool IsPriorityFile(string filePath, IReadOnlyList<string>? extensions)
        {
            if (extensions == null || extensions.Count == 0) return false;
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return extensions.Any(e => ext == (e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()));
        }

        // ===== FILE PROCESSING =====
        protected static async Task ProcessOneFileAsync(BackupJob job, string sourceFile, int thresholdKo,
            IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted,
            JobExecutionContext? context, Action<string, string>? onFileStarted = null)
        {
            var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
            var targetFile = Path.Combine(job.TargetDirectory, relativePath);

            // ===== NOTIFY START =====
            onFileStarted?.Invoke(sourceFile, targetFile);

            long fileSize = FileUtils.GetFileSize(sourceFile);

            // ===== LARGE FILE COORDINATION =====
            await LargeFileTransferCoordinator.Instance.AcquireSlotIfLargeAsync(fileSize, thresholdKo, context?.Token ?? CancellationToken.None);

            var stopwatch = Stopwatch.StartNew();
            int cryptTime = 0;

            try
            {
                // ===== FILE COPY =====
                if (context != null)
                    await FileUtils.CopyFileAsync(sourceFile, targetFile, context.Token);
                else
                    FileUtils.CopyFile(sourceFile, targetFile);

                // ===== ENCRYPTION =====
                if (encryptionService.IsExtensionTargeted(targetFile))
                    cryptTime = await encryptionService.EncryptAsync(targetFile);

                // ===== LOGGING & STATE =====
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
                // ===== RELEASE SLOT =====
                LargeFileTransferCoordinator.Instance.ReleaseSlotIfLarge(fileSize, thresholdKo);
            }
        }
    }
}