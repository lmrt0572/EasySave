using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Models.Enums
{
    // ===== BACKUP STATUS =====
    public enum BackupStatus
    {
        Completed,
        Error,
        Stopped,    
        Running,    
        Paused      
    }
}