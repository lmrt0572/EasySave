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
    // ===== FULL BACKUP STRATEGY =====

namespace EasySave.Core.Strategies
        // ===== EXECUTION (Console compatibility) =====
{
    public class FullBackupStrategy : IBackupStrategy
    {
        // ===== EXECUTION (with pause/stop support + large file coordination) =====
        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted)
        {
            {
            await Execute(job, encryptionService, onFileCompleted, context: null!);
        }
            }

        public async Task Execute(BackupJob job, IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted, JobExecutionContext context)
        {
            // ===== SOURCE DIRECTORY CHECK =====
            if (!FileUtils.DirectoryExists(job.SourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            var files = FileUtils.GetAllFilesRecursive(job.SourceDirectory).ToList();
            int thresholdKo = context?.LargeFileThresholdKo ?? 0;
            var priorityExtensions = context?.PriorityExtensions;
            bool usePriorityRule = context != null && priorityExtensions != null && priorityExtensions.Count > 0;

            if (!usePriorityRule)
            {
                foreach (var sourceFile in files)
                {
                    // ===== PAUSE / STOP (wait if paused by monitor or user; throw if stop requested) =====
                    context?.ThrowIfStoppedOrWaitIfPaused();
                    await ProcessOneFileAsync(job, sourceFile, thresholdKo, encryptionService, onFileCompleted, context);
                }
                return;
            }

            // ===== PRIORITY RULE: one loop — priority files processed now, others deferred =====
            PriorityFileCoordinator.Instance.RegisterJob(context!.JobName);
            var deferredFiles = new List<string>();
            try
            {
                foreach (var sourceFile in files)
                {
                    // ===== PAUSE / STOP (wait if paused; throw if stopped) =====
                    context.ThrowIfStoppedOrWaitIfPaused();

                    // ===== PRIORITY FILE ? Yes → process now ; No → leave for later =====
                    if (IsPriorityFile(sourceFile, priorityExtensions))
                        await ProcessOneFileAsync(job, sourceFile, thresholdKo, encryptionService, onFileCompleted, context);
                    else
                        deferredFiles.Add(sourceFile);
                }

                // ===== BARRIER: no non-priority transfer until all jobs have finished their priority phase =====
                PriorityFileCoordinator.Instance.NotifyPriorityPhaseDone(context.JobName);
                await PriorityFileCoordinator.Instance.WaitUntilCanTransferNonPriorityAsync(context.Token);

                foreach (var sourceFile in deferredFiles)
                {
                    // ===== PAUSE / STOP (wait if paused; throw if stopped) =====
                    context.ThrowIfStoppedOrWaitIfPaused();
                    await ProcessOneFileAsync(job, sourceFile, thresholdKo, encryptionService, onFileCompleted, context);
                }
            }
            finally
            {
                PriorityFileCoordinator.Instance.UnregisterJob(context.JobName);
            }
        }

        private static bool IsPriorityFile(string filePath, IReadOnlyList<string> extensions)
        {
            if (extensions == null || extensions.Count == 0) return false;
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) return false;
            foreach (var e in extensions)
            {
                var n = (e ?? "").Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(n)) continue;
                if (!n.StartsWith(".")) n = "." + n;
                if (ext == n) return true;
            }
            return false;
        }

        private static async Task ProcessOneFileAsync(BackupJob job, string sourceFile, int thresholdKo,
            IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted,
            JobExecutionContext? context)
        {
            var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
            var targetFile = Path.Combine(job.TargetDirectory, relativePath);
            long fileSize = FileUtils.GetFileSize(sourceFile);

            // ===== LARGE FILE ? Wait for slot if size > n Ko (parallel transfer restriction) =====
            await LargeFileTransferCoordinator.Instance.AcquireSlotIfLargeAsync(fileSize, thresholdKo, context?.Token ?? CancellationToken.None);

            var stopwatch = Stopwatch.StartNew();
            int cryptTime = 0;
            try
            {
                // ===== COPY FILE =====
                if (context != null)
                    await FileUtils.CopyFileAsync(sourceFile, targetFile, context.Token);
                else
                    FileUtils.CopyFile(sourceFile, targetFile);

                stopwatch.Stop();
                // ===== ENCRYPT IF EXTENSION TARGETED (CryptoSoft) =====
                if (encryptionService.IsExtensionTargeted(targetFile))
                    cryptTime = await encryptionService.EncryptAsync(targetFile);
                // ===== NOTIFY COMPLETION (state + log) =====
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
                // ===== RELEASE LARGE FILE SLOT IF USED =====
                LargeFileTransferCoordinator.Instance.ReleaseSlotIfLarge(fileSize, thresholdKo);
            }
        }
    }
}
