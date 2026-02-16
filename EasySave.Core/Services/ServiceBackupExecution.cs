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

        // ===== PUBLIC METHODS =====

        /// <summary>Execute backup (Overload for CLI/Console compatibility)</summary>
        public async Task Execute(BackupJob job, string businessSoftwareName)
        {
            await Execute(job, businessSoftwareName, CancellationToken.None);
        }

        /// <summary>Main Execute method with Business Software detection and CancellationToken support</summary>
        public async Task Execute(BackupJob job, string businessSoftwareName, CancellationToken cancellationToken)
        {
            // 1. Initial check: Stop before starting if software is already running
            if (FileUtils.IsProcessRunning(businessSoftwareName))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n  [STOP] Business software detected: {businessSoftwareName}. Backup aborted.");
                Console.ResetColor();

                // FIX: Utiliser SetStopped() au lieu de SetError() pour le logiciel métier
                var stoppedState = new BackupJobState(job.Name);
                stoppedState.SetStopped();
                _stateService.UpdateJobState(stoppedState);
                StateUpdated?.Invoke(stoppedState);

                LogBusinessSoftwareEvent(job.Name, businessSoftwareName);
                return;
            }

            // --- Initialization & Pre-Scan ---
            var state = new BackupJobState(job.Name);
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            _totalFiles = files.Length;
            _completedFiles = 0;

            // --- State Startup & Persistence ---
            state.StartBackup(_totalFiles, totalSize);
            _stateService.UpdateJobState(state);
            StateUpdated?.Invoke(state);

            Console.WriteLine($"\n  Starting: {job.Name} ({_totalFiles} files)");
            DisplayProgressBar(job.Name);

            // --- Strategy Execution Loop ---
            try
            {
                await _strategy.Execute(job, _encryptionService, (source, target, size, timeMs, cryptTime) =>
                {
                    // 2. Live Check: Business software opened OR GUI cancellation requested
                    if (FileUtils.IsProcessRunning(businessSoftwareName) || cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException($"[ABORT] Backup interrupted for {job.Name}.");
                    }

                    _currentFile = source;
                    _completedFiles++;

                    DisplayProgressBar(job.Name);

                    // --- Real-Time State Update ---
                    if (timeMs < 0)
                    {
                        state.SetError();
                    }
                    else
                    {
                        // FIX: UpdateCurrentFile AVANT CompleteFile pour que les fichiers soient renseignés
                        state.UpdateCurrentFile(source, target);
                        state.CompleteFile(size);
                    }

                    _stateService.UpdateJobState(state);
                    StateUpdated?.Invoke(state);

                    // --- Activity Logging ---
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

                // --- Normal Job Finalization ---
                // FIX: Finish() met correctement le statut à Completed (statut 2)
                state.Finish();
                _stateService.UpdateJobState(state);
                StateUpdated?.Invoke(state);

                DisplayProgressComplete(job.Name);
            }
            catch (OperationCanceledException)
            {
                // FIX: Utiliser SetStopped() (statut 4 = Stopped) au lieu de mettre Error
                state.SetStopped();
                _stateService.UpdateJobState(state);
                StateUpdated?.Invoke(state);

                LogBusinessSoftwareEvent(job.Name, businessSoftwareName);

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  ⚠ {job.Name} stopped (software detected or user cancellation)");
                Console.ResetColor();

                // Re-throw so caller (WpfViewModel) can handle it too
                throw;
            }
            catch (Exception ex)
            {
                // FIX: Seules les vraies erreurs utilisent SetError() (statut 3 = Error)
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
            }
        }

        // ===== BUSINESS SOFTWARE LOGGING (#15) =====
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

        // ===== PROGRESS BAR =====
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