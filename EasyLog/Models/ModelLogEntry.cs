using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Models
{
    public class ModelLogEntry
    {
        public DateTime Timestamp { get; set; }

        public string JobName { get; set; } = string.Empty;

        public string SourcePath { get; set; } = string.Empty;

        public string TargetPath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public long TransferTimeMs { get; set; }

    }
}
