using System;
using System.Collections.Generic;
using System.Text;

using EasySave.Models;

namespace EasySave.Services
{
    public class ServiceBackupScheduler
    {
        private readonly ServiceBackupExecution _executionService;

        public ServiceBackupScheduler(ServiceBackupExecution executionService)
        {
            _executionService = executionService;
        }

        public void Execute(BackupJob job)
        {
            _executionService.Execute(job);
        }
        public void RunSequential(IEnumerable<BackupJob> jobs)
        {
            foreach (var job in jobs)
            {
                _executionService.Execute(job);
            }
        }
    }
}