using System;
using System.IO;
using System.Linq;
using EasyLog.Models;
using EasyLog.Services;
using EasySave.Models;
using EasySave.Models.Enums;
using EasySave.Strategies;
using EasySave.Utils;

namespace EasySave.Services
{
    public class ServiceBackupExecution
    {
        private readonly IBackupStrategy _strategy;
        private readonly ILogService _logService;
        private readonly IStateService _stateService;

        public event Action<BackupJobState>? StateUpdated;

        public ServiceBackupExecution(IBackupStrategy strategy, ILogService logService, IStateService stateService)
        {
            _strategy = strategy;
            _logService = logService;
            _stateService = stateService;
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        }

        public void Execute(BackupJob job)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            // ----- INITIAL STATE -----
            var state = new BackupJobState(job.Name);

            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            state.StartBackup(files.Length, totalSize);

            StateUpdated?.Invoke(state);
            _stateService.UpdateJobState(state);

            // ----- STRATEGY EXECUTION WITH CALLBACK -----
            _strategy.Execute(job, (source, target, size, timeMs) =>
            {
                if (timeMs < 0)
                {
                    // Error case
                    state.SetError();
                }
                else
                {
                    // Success case: update current file and progression
                    state.UpdateCurrentFile(source, target);
                    state.CompleteFile(size);
                }

                // Build log entry with UNC-normalized paths
                var entry = new ModelLogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = job.Name,
                    SourcePath = FileUtils.GetUNCPath(source),
                    TargetPath = FileUtils.GetUNCPath(target),
                    FileSize = size,
                    TransferTimeMs = timeMs
                };

                _logService.Write(entry);

                StateUpdated?.Invoke(state);
                _stateService.UpdateJobState(state);
            });

            // ----- FINALIZATION -----
            if (state.Status != BackupStatus.Error)
            {
                state.Finish();
            }

            _logService.Flush();

            StateUpdated?.Invoke(state);
            _stateService.UpdateJobState(state);
        }
    }
}