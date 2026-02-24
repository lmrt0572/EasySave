using EasyLog.Models;
using EasyLog.Services;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;
using EasySave.Core.Strategies;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasySave.Core.Services
{
    public class ServiceBackupExecution
    {
        private readonly IBackupStrategy _strategy;
        private readonly ILogService _logService;
        private readonly IStateService _stateService;
        private readonly IEncryptionService _encryptionService;

        public event Action<BackupJobState>? StateUpdated;

        public ServiceBackupExecution(IBackupStrategy strategy, ILogService logService,
            IStateService stateService, IEncryptionService encryptionService)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        public async Task Execute(BackupJob job, JobExecutionContext context)
        {
            var files = Directory.GetFiles(job.SourceDirectory, "*", SearchOption.AllDirectories);
            long totalSize = files.Sum(f => new System.IO.FileInfo(f).Length);
            int totalFiles = files.Length;
            int completedFiles = 0;

            var state = new BackupJobState(job.Name);
            state.StartBackup(totalFiles, totalSize);
            state.Status = BackupStatus.Running;
            Notify(state);

            try
            {
                await _strategy.Execute(job, _encryptionService, (source, target, size, timeMs, cryptTime) =>
                {
                    completedFiles++;

                    if (timeMs < 0)
                        state.SetError();
                    else
                    {
                        state.UpdateCurrentFile(source, target);
                        state.CompleteFile(size);
                        state.Status = BackupStatus.Running;
                    }

                    Notify(state);

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
                Notify(state);
            }
            catch (OperationCanceledException)
            {
                state.SetStopped();
                Notify(state);

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
                Notify(state);
                throw;
            }
            finally
            {
                _logService.Flush();
                _stateService.Flush();
            }
        }

        private void Notify(BackupJobState state)
        {
            _stateService.UpdateJobState(state);
            StateUpdated?.Invoke(state);
        }
    }
}