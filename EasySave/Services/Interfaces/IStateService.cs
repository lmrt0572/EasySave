using System;
using System.Collections.Generic;
using System.Text;
using EasySave.Models;

namespace EasySave.Services
{
    // ===== STATE SERVICE INTERFACE =====
    public interface IStateService
    {
        void UpdateJobState(BackupJobState state);
    }
}
