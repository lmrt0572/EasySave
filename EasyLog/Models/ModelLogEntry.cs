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

        public string SourcePath { get; set; } = string.Empty;

        public string TargetPath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public long TransferTimeMs { get; set; }

        public string? EventType { get; set; }

        public string? EventDetails { get; set; }
      
        public int EncryptionTimeMs { get; set; }
    }
}
