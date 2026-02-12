using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Models
{
    // ===== LOG ENTRY =====
    public class ModelLogEntry
    {
        // ===== FIELDS =====
        public DateTime Timestamp { get; set; }

        public string JobName { get; set; } = string.Empty;

        // Source and target paths are provided by EasySave (already normalized/UNC)
        public string SourcePath { get; set; } = string.Empty;

        public string TargetPath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public long TransferTimeMs { get; set; }

        // V2.0 - Event type for special log entries (e.g. "Business Software Detected")
        public string? EventType { get; set; }

        // V2.0 - Additional info (e.g. detected process name)
        public string? EventDetails { get; set; }
    }
}
