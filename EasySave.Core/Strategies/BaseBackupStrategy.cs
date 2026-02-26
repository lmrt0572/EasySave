using EasySave.Core.Models;
using EasySave.Core.Services;
using EasySave.Core.Utils;
using System.Diagnostics;

namespace EasySave.Core.Strategies
{
    public abstract class BaseBackupStrategy : IBackupStrategy
    {
        public async Task Execute(BackupJob job, IEncryptionService encryptionService,
            Action<string, string, long, long, int> onFileCompleted,
            JobExecutionContext context,
            Action<string, string>? onFileStarted = null)
        {
            // ===== VALIDATION: Ensure source directory exists before starting =====
            if (!FileUtils.DirectoryExists(job.SourceDirectory))
                throw new DirectoryNotFoundException($"Source directory not found: {job.SourceDirectory}");

            // ===== PREPARATION: Retrieve files list and environmental constraints (threshold, priorities) =====
            var filesToCopy = GetFilesToProcess(job);
            int thresholdKo = context.LargeFileThresholdKo;
            var priorityExtensions = context.PriorityExtensions;
            bool usePriorityRule = priorityExtensions != null && priorityExtensions.Count > 0;

            // ===== STANDARD EXECUTION: Process files sequentially if no priority rules are defined =====
            if (!usePriorityRule)
            {
                foreach (var file in filesToCopy)
                {
                    // Check for Pause/Stop signals from the UI or Business Software Monitor
                    context.ThrowIfStoppedOrWaitIfPaused();
                    await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
                }
                return;
            }

            // ===== PRIORITY EXECUTION: Each job processes its priority files first, then non-priority (no cross-job wait) =====
            var deferredFiles = new List<string>();

            // First pass: Process only priority files
            foreach (var file in filesToCopy)
            {
                context.ThrowIfStoppedOrWaitIfPaused();

                if (IsPriorityFile(file, priorityExtensions))
                    await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
                else
                    deferredFiles.Add(file);
            }

            // Second pass: Process remaining non-priority files
            foreach (var file in deferredFiles)
            {
                context.ThrowIfStoppedOrWaitIfPaused();
                await ProcessOneFileAsync(job, file, thresholdKo, encryptionService, onFileCompleted, context, onFileStarted);
            }
        }

        // ===== ABSTRACTION: Implemented by Full/Differential strategies to filter the file list =====
        protected abstract List<string> GetFilesToProcess(BackupJob job);

        // ===== UTILS: Extension matching logic for priority handling =====
        protected static bool IsPriorityFile(string filePath, IReadOnlyList<string>? extensions)
        {
            if (extensions == null || extensions.Count == 0) return false;
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return extensions.Any(e => ext == (e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()));
        }

        // ===== FILE ENGINE: Core logic for transfer, coordination, encryption, and logging =====
        protected static async Task ProcessOneFileAsync(BackupJob job, string sourceFile, int thresholdKo,
            IEncryptionService encryptionService, Action<string, string, long, long, int> onFileCompleted,
            JobExecutionContext context, Action<string, string>? onFileStarted = null)
        {
            var relativePath = Path.GetRelativePath(job.SourceDirectory, sourceFile);
            var targetFile = Path.Combine(job.TargetDirectory, relativePath);

            onFileStarted?.Invoke(sourceFile, targetFile);

            long fileSize = FileUtils.GetFileSize(sourceFile);

            // ===== NETWORK COORDINATION: Limit bandwidth usage for large files across all jobs =====
            await LargeFileTransferCoordinator.Instance.AcquireSlotIfLargeAsync(
                fileSize, thresholdKo, context.Token);

            var stopwatch = Stopwatch.StartNew();
            int cryptTime = 0;

            try
            {
                // ===== DATA TRANSFER: Physical copy with cancellation support =====
                await FileUtils.CopyFileAsync(sourceFile, targetFile, context.Token);
                stopwatch.Stop();

                // ===== SECURITY: Apply CryptoSoft encryption if extension matches configuration =====
                if (encryptionService.IsExtensionTargeted(targetFile))
                    cryptTime = await encryptionService.EncryptAsync(targetFile);

                // Callback to update progress UI and Daily Logs
                onFileCompleted(sourceFile, targetFile, fileSize, stopwatch.ElapsedMilliseconds, cryptTime);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception)
            {
                if (stopwatch.IsRunning) stopwatch.Stop();
                onFileCompleted(sourceFile, targetFile, 0, -1, -1); // Log as error
                throw;
            }
            finally
            {
                // Always release the coordination slot, even if transfer failed
                LargeFileTransferCoordinator.Instance.ReleaseSlotIfLarge(fileSize, thresholdKo);
            }
        }
    }
}