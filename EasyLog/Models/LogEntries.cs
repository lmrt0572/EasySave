using System.Collections.Generic;
using System.Xml.Serialization;

namespace EasyLog.Models
{
    // ===== LOG ENTRIES ROOT =====
    [XmlRoot("LogEntries")]
    public class LogEntries
    {
        [XmlElement("LogEntry")]
        public List<ModelLogEntry> Entries { get; set; } = new List<ModelLogEntry>();
    }
}

