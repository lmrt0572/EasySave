// ===== EasySave.Core/Models/AppConfig.cs =====

using EasyLog.Models;
using System.Collections.Generic;

namespace EasySave.Core.Models
{
    public class AppConfig
    {
        public string EncryptionKey { get; set; } = "Prosoft123";
        public List<string> EncryptionExtensions { get; set; } = new() { ".txt", ".md", ".pdf" };
        public string BusinessSoftwareName { get; set; } = "CalculatorApp";
        public LogFormat LogFormat { get; set; } = LogFormat.Json;
        public List<BackupJob> Jobs { get; set; } = new();
    }
}
