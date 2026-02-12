using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace EasyLog.Models
{
    // ===== LOG ENTRY =====
    [XmlRoot("LogEntry")]
    public class ModelLogEntry
    {
        // ===== FIELDS =====
        [XmlElement("Timestamp")]
        public DateTime Timestamp { get; set; }

        [XmlElement("JobName")]
        public string JobName { get; set; } = string.Empty;

        // Source and target paths are provided by EasySave (already normalized/UNC)
        [XmlElement("SourcePath")]
        public string SourcePath { get; set; } = string.Empty;

        [XmlElement("TargetPath")]
        public string TargetPath { get; set; } = string.Empty;

        [XmlElement("FileSize")]
        public long FileSize { get; set; }

        [XmlElement("TransferTimeMs")]
        public long TransferTimeMs { get; set; }
    }
}
