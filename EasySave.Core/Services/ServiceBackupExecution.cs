using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.Strategies;
using EasySave.Core.Utils;

namespace EasySave.Core.Services
{
    public class ServiceBackupExecution
    {
        // ===== PRIVATE FIELDS =====
        private readonly IBackupStrategy _strategy;
        private readonly ILogService _logService;
        private readonly IStateService _stateService;
        private readonly IEncryptionService _encryptionService;
        private int _totalFiles;
        private int _completedFiles;
        private string _currentFile = "";

        // ===== EVENTS =====
        public event Action<BackupJobState>? StateUpdated;

        // ===== CONSTRUCTOR =====
        public ServiceBackupExecution(IBackupStrategy strategy, ILogService logService, IStateService stateService, IEncryptionService encryptionService)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        // ===== V2 PUBLIC METHODS (Console compatibility) =====

        public async Task Execute(BackupJob job, string businessSoftwareName)
        {
            await Execute(job, businessSoftwareName, CancellationToken.None);
        }

        public async Task Execute(BackupJob job, string businessSoftwareName, CancellationToken cancellationToken)
        {
            if (FileUtils.IsProcessRunning(businessSoftwareName))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n  [STOP] Business software detected: {businessSoftwareName}. Backup aborted.");
                Console.ResetColor();

                var stoppedState = new BackupJobState(job.Name);
                stoppedState.SetStopped();
                _stateService.UpdateJobState(stoppedState);
                StateUpdated?.Invoke(stoppedState);

                LogBusinessSoftwareEvent(job.Name, businessSoftwareName);
                return;
            }

            var state = new BackupJobState(job.Name);
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            _totalFiles = files.Length;
            _completedFiles = 0;

            state.StartBackup(_totalFiles, totalSize);
            _stateService.UpdateJobState(state);
            StateUpdated?.Invoke(state);

            Console.WriteLine($"\n  Starting: {job.Name} ({_totalFiles} files)");
            DisplayProgressBar(job.Name);

            try
            {
                await _strategy.Execute(job, _encryptionService, (source, target, size, timeMs, cryptTime) =>
                {
                    if (FileUtils.IsProcessRunning(businessSoftwareName) || cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException($"[ABORT] Backup interrupted for {job.Name}.");
                    }

                    _currentFile = source;
                    _completedFiles++;

                    DisplayProgressBar(job.Name);

                    if (timeMs < 0)
                    {
                        state.SetError();
                    }
                    else
                    {
                        state.UpdateCurrentFile(source, target);
                        state.CompleteFile(size);
                    }

                    _stateService.UpdateJobState(state);
                    StateUpdated?.Invoke(state);

                    _logService.Write(new ModelLogEntry
                    {
                        Timestamp = DateTime.Now,
                        JobName = job.Name,
                        SourcePath = source,
                        TargetPath = target,
                        FileSize = size,
                        TransferTimeMs = timeMs,
                        EncryptionTimeMs = cryptTime
                    });
                });

                state.Finish();
                _stateService.UpdateJobState(state);
                StateUpdated?.Invoke(state);

                DisplayProgressComplete(job.Name);
            }
            catch (OperationCanceledException)
            {
                state.SetStopped();
                _stateService.UpdateJobState(state);
                StateUpdated?.Invoke(state);

                LogBusinessSoftwareEvent(job.Name, businessSoftwareName);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠ {job.Name} stopped (software detected or user cancellation)");
                Console.ResetColor();

                throw;
            }
            catch (Exception ex)
            {
                state.SetError();
                _stateService.UpdateJobState(state);
                StateUpdated?.Invoke(state);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n  ✕ {job.Name} error: {ex.Message}");
                Console.ResetColor();

                throw;
            }
            finally
            {
                _logService.Flush();
                _stateService.Flush();
            }
        }

        // ===== V3 PUBLIC METHOD (WPF with JobExecutionContext) =====


        /// Execute a backup job with V3 pause/resume/stop support.
        /// The JobExecutionContext handles pause gating and cancellation.

        public async Task Execute(BackupJob job, JobExecutionContext context)
        {
            var state = new BackupJobState(job.Name);
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            _totalFiles = files.Length;
            _completedFiles = 0;

            state.StartBackup(_totalFiles, totalSize);
            state.Status = BackupStatus.Running;
            _stateService.UpdateJobState(state);
            StateUpdated?.Invoke(state);

            try
            {
                await _strategy.Execute(job, _encryptionService, (source, target, size, timeMs, cryptTime) =>
                {
                    // V3 - The pause/stop check is already done in the strategy via context.ThrowIfStoppedOrWaitIfPaused()
                    // Here we just update state and log

                    _currentFile = source;
                    _completedFiles++;

                    if (timeMs < 0)
                    {
                        state.SetError();
                    }
                    else
                    {
                        state.UpdateCurrentFile(source, target);
                        state.CompleteFile(size);
                        state.Status = BackupStatus.Running;
                    }

                    _stateService.UpdateJobState(state);
                    StateUpdated?.Invoke(state);

                    _logService.Write(new ModelLogEntry
                    {
                        Timestamp = DateTime.Now,
                        JobName = job.Name,
                        SourcePath = source,
                        TargetPath = target,
                        FileSize = size,
                        TransferTimeMs = timeMs,
                        EncryptionTimeMs = cryptTime
                    });
                }, context);

                state.Finish();
                _stateService.UpdateJobState(state);
                StateUpdated?.Invoke(state);
            }
            catch (OperationCanceledException)
            {
                state.SetStopped();
                _stateService.UpdateJobState(state);
                StateUpdated?.Invoke(state);

                _logService.Write(new ModelLogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = job.Name,
                    EventType = "Job Stopped",
                    EventDetails = "Stopped by user or system",
                    SourcePath = string.Empty,
                    TargetPath = string.Empty,
                    FileSize = 0,
                    TransferTimeMs = 0,
                    EncryptionTimeMs = 0
                });

                throw;
            }
            catch (Exception)
            {
                state.SetError();
                _stateService.UpdateJobState(state);
                StateUpdated?.Invoke(state);
                throw;
            }
            finally
            {
                _logService.Flush();
                _stateService.Flush();
            }
        }

        // ===== BUSINESS SOFTWARE LOGGING =====
        private void LogBusinessSoftwareEvent(string jobName, string processName)
        {
            _logService.Write(new ModelLogEntry
            {
                Timestamp = DateTime.Now,
                JobName = jobName,
                EventType = "Business Software Detected",
                EventDetails = $"Process: {processName}",
                SourcePath = string.Empty,
                TargetPath = string.Empty,
                FileSize = 0,
                TransferTimeMs = 0,
                EncryptionTimeMs = 0
            });
        }

        // ===== PROGRESS BAR (Console) =====
        private void DisplayProgressBar(string jobName)
        {
            double progress = _totalFiles > 0 ? ((double)_completedFiles / _totalFiles) * 100 : 100;
            int barWidth = 30;
            int filled = (int)(progress / 100 * barWidth);
            int empty = barWidth - filled;

            string bar = new string('█', filled) + new string('░', empty);

            string displayFile = _currentFile;
            if (displayFile.Length > 25)
                displayFile = "..." + displayFile.Substring(displayFile.Length - 22);

            Console.Write($"\r  [{bar}] {progress,5:F1}% | {_completedFiles}/{_totalFiles} | {displayFile,-28}");
        }

        private void DisplayProgressComplete(string jobName)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {jobName} completed!");
            Console.ResetColor();
        }
    }
}