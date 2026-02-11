using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            _strategy = strategy;
            _logService = logService;
            _stateService = stateService;
            _encryptionService = encryptionService;
        }

        // ===== PUBLIC METHODS =====
        public async Task Execute(BackupJob job)
        {
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
            await _strategy.Execute(job, _encryptionService, (source, target, size, timeMs, cryptTime) =>
            {
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

            // --- Job Finalization ---
            state.Finish();
            _stateService.UpdateJobState(state);
            StateUpdated?.Invoke(state);

            DisplayProgressComplete(job.Name);
            _logService.Flush();
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