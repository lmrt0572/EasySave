using EasyLog.Services;
using EasyLog.Models;
using EasySave.Models;
using EasySave.Models.Enums;
using EasySave.Services.Interfaces;
using EasySave.Strategies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        }

        public void Execute(BackupJob job)
        {
            var state = new BackupJobState(job.Name);

            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            long totalSize = files.Sum(f => new FileInfo(f).Length);

            state.StartBackup(files.Length, totalSize);

            StateUpdated?.Invoke(state);
            _stateService.UpdateJobState(state);

            _strategy.Execute(job, (source, target, size, timeMs) =>
            {
                if (timeMs < 0)
                {
                    state.SetError();
                }
                else
                {
                    state.UpdateCurrentFile(source, target);
                    state.CompleteFile(size);
                }

                _logService.Write(new ModelLogEntry
                {
                    Timestamp = DateTime.Now,
                    JobName = job.Name,
                    SourcePath = source,
                    TargetPath = target,
                    FileSize = size,
                    TransferTimeMs = timeMs
                });

                StateUpdated?.Invoke(state);
                _stateService.UpdateJobState(state);
            });

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