using System;
using System.Collections.Generic;
using System.Text;

using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    // ===== BACKUP SCHEDULER =====
    public class ServiceBackupScheduler
    {
        // ===== PRIVATE MEMBERS =====
        private readonly ServiceBackupExecution _executionService;

        // ===== CONSTRUCTOR =====
        public ServiceBackupScheduler(ServiceBackupExecution executionService)
        {
            _executionService = executionService;
        }

        // ===== EXECUTION =====
        public void Execute(BackupJob job, string businessSoftwareName)
        {
            _ = _executionService.Execute(job, businessSoftwareName);
        }

        public async Task RunSequential(IEnumerable<BackupJob> jobs, string businessSoftwareName)
        {
            foreach (var job in jobs)
            {
                await _executionService.Execute(job, businessSoftwareName);
            }
        }
    }
}