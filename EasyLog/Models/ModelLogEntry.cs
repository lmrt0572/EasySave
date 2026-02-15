using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace EasyLog.Models
{
    // ===== LOG ENTRY =====
    [XmlRoot("LogEntry")]
    public class ModelLogEntry
    {
        // ===== FIELDS =====
        public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";


        [XmlIgnore]
        [JsonIgnore]
        public DateTime Timestamp { get; set; }

        [XmlElement("Timestamp", Order = 1)]
        [JsonPropertyName("Timestamp")]
        public string TimestampString
        {
            get => Timestamp.ToString(TimestampFormat);
            set { if (DateTime.TryParse(value, out var t)) Timestamp = t; }
        }

        [XmlElement("JobName", Order = 2)]
        public string JobName { get; set; } = string.Empty;

        [XmlElement("SourcePath", Order = 3)]
        public string SourcePath { get; set; } = string.Empty;

        [XmlElement("TargetPath", Order = 4)]
        public string TargetPath { get; set; } = string.Empty;

        [XmlElement("FileSize", Order = 5)]
        public long FileSize { get; set; }

        [XmlElement("TransferTimeMs", Order = 6)]
        public long TransferTimeMs { get; set; }

        [XmlElement("EncryptionTimeMs", Order = 7)]
        public int EncryptionTimeMs { get; set; }

        [XmlElement("EventType", Order = 8)]
        public string? EventType { get; set; }

        [XmlElement("EventDetails", Order = 9)]
        public string? EventDetails { get; set; }
    }
}
