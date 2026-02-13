using EasySave.Core.Models.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasySave.Core.Models
{
    // ===== BACKUP JOB =====
    public class BackupJob
    {
        // ===== FIELDS =====
        public string Name { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
        public BackupType Type { get; set; }

        // ===== CONSTRUCTOR =====
        public BackupJob(string name, string source, string target, BackupType type)
        {
            Name = name;
            SourceDirectory = source;
            TargetDirectory = target;
            Type = type;
        }
    }
}
