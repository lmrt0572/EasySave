using System;
using System.Collections.Generic;
using System.Text;
using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    // ===== STATE SERVICE INTERFACE =====
    public interface IStateService
    {
        void UpdateJobState(BackupJobState state);
    }
}
