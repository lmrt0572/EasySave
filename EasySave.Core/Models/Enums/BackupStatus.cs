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
        Stopped // V2.0 - Stopped by business software detection
    }
}
