using Xunit;
using EasyLog.Models;
using EasyLog.Services.Strategies;

namespace EasySave.Tests.Services
{
    // ===== LOG STRATEGY TESTS =====
    public class LogStrategyTests : IDisposable
    {
        private readonly string _testDir;

        public LogStrategyTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"EasySaveTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        private ModelLogEntry CreateSampleEntry() => new()
        {
            Timestamp = new DateTime(2026, 2, 20, 14, 0, 0),
            JobName = "TestBackup",
            SourcePath = @"C:\Source\file.txt",
            TargetPath = @"D:\Target\file.txt",
            FileSize = 1024,
            TransferTimeMs = 5,
            EncryptionTimeMs = 0,
            Username = "testuser",
            MachineName = "TESTPC"
        };

        // ==========================================
        // ===== JSON STRATEGY =====
        // ==========================================

        [Fact]
        public void JsonStrategy_GetFileExtension_ReturnsJson()
        {
            var strategy = new JsonLogStrategy();

            Assert.Equal(".json", strategy.GetFileExtension());
        }

        [Fact]
        public void JsonStrategy_WriteEntry_ProducesValidJson()
        {
            var strategy = new JsonLogStrategy();
            var entry = CreateSampleEntry();

            using var sw = new StringWriter();
            using var writer = new StreamWriter(new MemoryStream());

            // Write to a temp file and verify
            string filePath = Path.Combine(_testDir, "test.json");
            strategy.WriteEntries(filePath, new List<ModelLogEntry> { entry });

            string content = File.ReadAllText(filePath);
            Assert.Contains("TestBackup", content);
            Assert.Contains("1024", content);
            Assert.Contains("testuser", content);
        }

        [Fact]
        public void JsonStrategy_WriteEntries_MultipleEntries_AllPresent()
        {
            var strategy = new JsonLogStrategy();
            var entries = new List<ModelLogEntry>
            {
                CreateSampleEntry(),
                new ModelLogEntry
                {
                    Timestamp = new DateTime(2026, 2, 20, 15, 0, 0),
                    JobName = "SecondJob",
                    SourcePath = @"C:\Other\data.csv",
                    TargetPath = @"D:\Other\data.csv",
                    FileSize = 2048
                }
            };

            string filePath = Path.Combine(_testDir, "multi.json");
            strategy.WriteEntries(filePath, entries);

            string content = File.ReadAllText(filePath);
            Assert.Contains("TestBackup", content);
            Assert.Contains("SecondJob", content);
        }

        // ==========================================
        // ===== XML STRATEGY =====
        // ==========================================

        [Fact]
        public void XmlStrategy_GetFileExtension_ReturnsXml()
        {
            var strategy = new XmlLogStrategy();

            Assert.Equal(".xml", strategy.GetFileExtension());
        }

        [Fact]
        public void XmlStrategy_WriteEntries_ProducesValidXml()
        {
            var strategy = new XmlLogStrategy();
            var entry = CreateSampleEntry();

            string filePath = Path.Combine(_testDir, "test.xml");
            strategy.WriteEntries(filePath, new List<ModelLogEntry> { entry });

            string content = File.ReadAllText(filePath);
            Assert.Contains("<?xml", content);
            Assert.Contains("TestBackup", content);
            Assert.Contains("1024", content);
        }

        [Fact]
        public void XmlStrategy_WriteEntries_MultipleEntries_AllPresent()
        {
            var strategy = new XmlLogStrategy();
            var entries = new List<ModelLogEntry>
            {
                CreateSampleEntry(),
                new ModelLogEntry
                {
                    Timestamp = new DateTime(2026, 2, 20, 15, 0, 0),
                    JobName = "AnotherJob",
                    SourcePath = @"C:\Other\file.txt",
                    TargetPath = @"D:\Other\file.txt",
                    FileSize = 512
                }
            };

            string filePath = Path.Combine(_testDir, "multi.xml");
            strategy.WriteEntries(filePath, entries);

            string content = File.ReadAllText(filePath);
            Assert.Contains("TestBackup", content);
            Assert.Contains("AnotherJob", content);
        }
    }
}