using System;
using System.Collections.Generic;
using System.Text;

using EasySave.Models;

namespace EasySave.Services
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