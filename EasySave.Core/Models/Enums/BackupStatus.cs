using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Models.Enums
{
    // ===== BACKUP STATUS =====
    public enum BackupStatus
    {
        Inactive,
        Active,
        Completed,
        Error,
        Stopped,    
        Running,    
        Paused      
    }
}