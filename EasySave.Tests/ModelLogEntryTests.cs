using Xunit;
using EasyLog.Models;

namespace EasySave.Tests.Models
{
    // ===== MODEL LOG ENTRY TESTS =====
    public class ModelLogEntryTests
    {
        // ==========================================
        // ===== TIMESTAMP FORMATTING =====
        // ==========================================

        [Fact]
        public void TimestampString_Get_FormatsCorrectly()
        {
            var entry = new ModelLogEntry
            {
                Timestamp = new DateTime(2026, 2, 20, 14, 30, 45)
            };

            Assert.Equal("2026-02-20 14:30:45", entry.TimestampString);
        }

        [Fact]
        public void TimestampString_Set_ParsesCorrectly()
        {
            var entry = new ModelLogEntry();

            entry.TimestampString = "2026-02-20 14:30:45";

            Assert.Equal(new DateTime(2026, 2, 20, 14, 30, 45), entry.Timestamp);
        }

        [Fact]
        public void TimestampString_SetInvalidDate_DoesNotThrow()
        {
            var entry = new ModelLogEntry();

            // Should not throw; Timestamp stays default
            entry.TimestampString = "not-a-date";

            Assert.Equal(default(DateTime), entry.Timestamp);
        }

        // ==========================================
        // ===== DEFAULT VALUES =====
        // ==========================================

        [Fact]
        public void DefaultValues_AreEmptyOrZero()
        {
            var entry = new ModelLogEntry();

            Assert.Equal(string.Empty, entry.JobName);
            Assert.Equal(string.Empty, entry.SourcePath);
            Assert.Equal(string.Empty, entry.TargetPath);
            Assert.Equal(0, entry.FileSize);
            Assert.Equal(0, entry.TransferTimeMs);
            Assert.Equal(0, entry.EncryptionTimeMs);
            Assert.Null(entry.EventType);
            Assert.Null(entry.EventDetails);
            Assert.Equal(string.Empty, entry.Username);
            Assert.Equal(string.Empty, entry.MachineName);
        }

        // ==========================================
        // ===== PROPERTY ASSIGNMENT =====
        // ==========================================

        [Fact]
        public void AllProperties_CanBeSet()
        {
            var entry = new ModelLogEntry
            {
                JobName = "DailyBackup",
                SourcePath = @"C:\Source\file.txt",
                TargetPath = @"D:\Target\file.txt",
                FileSize = 1024,
                TransferTimeMs = 50,
                EncryptionTimeMs = 10,
                EventType = "FileCopied",
                EventDetails = "Success",
                Username = "admin",
                MachineName = "SERVER01"
            };

            Assert.Equal("DailyBackup", entry.JobName);
            Assert.Equal(@"C:\Source\file.txt", entry.SourcePath);
            Assert.Equal(@"D:\Target\file.txt", entry.TargetPath);
            Assert.Equal(1024, entry.FileSize);
            Assert.Equal(50, entry.TransferTimeMs);
            Assert.Equal(10, entry.EncryptionTimeMs);
            Assert.Equal("FileCopied", entry.EventType);
            Assert.Equal("Success", entry.EventDetails);
            Assert.Equal("admin", entry.Username);
            Assert.Equal("SERVER01", entry.MachineName);
        }
    }
}