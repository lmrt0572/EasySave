using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Models;
using EasySave.Models.Enums;
using EasySave.Strategies;

namespace EasySave.Services
{
    // ===== BACKUP EXECUTION =====
    public class ServiceBackupExecution
    {
        // ===== PRIVATE MEMBERS =====
        private readonly IBackupStrategy _strategy;
        private readonly ILogService _logService;
        private readonly IStateService _stateService;

        // ===== PROGRESS TRACKING =====
        private int _totalFiles;
        private int _completedFiles;
        private string _currentFile = "";
        // ===== EVENTS =====
        public event Action<BackupJobState>? StateUpdated;

        // ===== CONSTRUCTOR =====
        public ServiceBackupExecution(IBackupStrategy strategy, ILogService logService, IStateService stateService)
        {
            _strategy = strategy;
            _logService = logService;
            _stateService = stateService;
        }

        // ===== EXECUTION =====
        public void Execute(BackupJob job)
        {
            // Scan source directory
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            _totalFiles = files.Length;
            _completedFiles = 0;

            Console.WriteLine($"  Starting: {job.Name} ({_totalFiles} files)");

            // INITIALISATION : Afficher une barre vide immédiatement
            DisplayProgressBar(job.Name);

            // Execute strategy
            _strategy.Execute(job, (source, target, size, timeMs) =>
            {
                _currentFile = source;
                _completedFiles++;

                // Affichage systématique
                DisplayProgressBar(job.Name);

                // Logique de log et d'état (ne pas oublier de mettre à jour l'état ici pour le JSON)
                _logService.Write(new ModelLogEntry { /* ... vos données ... */ });

                // IMPORTANT : Si vous utilisez IStateService, appelez-le ici pour que state.json soit juste
                var state = new BackupJobState(job.Name);
                state.UpdateCurrentFile(source, target);
                _stateService.UpdateJobState(state);
            });

            // Finish
            DisplayProgressComplete(job.Name);
            _logService.Flush();
        }
        // ==================== PROGRESS DISPLAY ====================

        private void DisplayProgressBar(string jobName)
        {
            double progress = _totalFiles > 0 ? ((double)_completedFiles / _totalFiles) * 100 : 100;
            int barWidth = 30;
            int filled = (int)(progress / 100 * barWidth);
            int empty = barWidth - filled;

            string bar = new string('█', filled) + new string('░', empty);

            // Truncate filename if too long
            string displayFile = _currentFile;
            if (displayFile.Length > 25)
                displayFile = "..." + displayFile.Substring(displayFile.Length - 22);

            // Overwrite current line with \r
            Console.Write($"\r  [{bar}] {progress,5:F1}% | {_completedFiles}/{_totalFiles} | {displayFile,-28}");
        }

        private void DisplayProgressComplete(string jobName)
        {
            Console.WriteLine(); // New line after progress bar
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {jobName} completed!");
            Console.ResetColor();
        }
    }
}