using Xunit;
using EasySave.Core.Models;
using EasyLog.Models;

namespace EasySave.Tests.Models
{
    // ===== APP CONFIG TESTS =====

    // Coverage: default values, property modification, Jobs collections,
    //           EncryptionExtensions, PriorityExtensions (v3).
    public class AppConfigTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            // All default values must match the constants defined in AppConfig
            var config = new AppConfig();

            Assert.Equal("Prosoft123", config.EncryptionKey);
            Assert.Equal(LogFormat.Json, config.LogFormat);
            Assert.Equal(LogMode.Local, config.LogMode);
            Assert.Equal("CalculatorApp", config.BusinessSoftwareName);
            Assert.Equal("http://localhost:8080/api/logs", config.DockerUrl);
            Assert.Equal(1000, config.LargeFileThresholdKo);
            Assert.NotNull(config.Jobs);
            Assert.Empty(config.Jobs);
        }

        [Fact]
        public void DefaultEncryptionExtensions_ContainExpectedTypes()
        {
            // Default encrypted extensions are .txt, .md, .pdf
            var config = new AppConfig();

            Assert.Contains(".txt", config.EncryptionExtensions);
            Assert.Contains(".md", config.EncryptionExtensions);
            Assert.Contains(".pdf", config.EncryptionExtensions);
            Assert.Equal(3, config.EncryptionExtensions.Count);
        }

        [Fact]
        public void DefaultPriorityExtensions_IsEmpty()
        {
            // No priority extensions by default (user configures them in v3)
            var config = new AppConfig();

            Assert.NotNull(config.PriorityExtensions);
            Assert.Empty(config.PriorityExtensions);
        }

        [Fact]
        public void PriorityExtensions_CanBePopulated()
        {
            // User can define priority extensions (ticket BASTIEN #12 / #5)
            var config = new AppConfig();
            config.PriorityExtensions.Add(".pdf");
            config.PriorityExtensions.Add(".docx");

            Assert.Equal(2, config.PriorityExtensions.Count);
            Assert.Contains(".pdf", config.PriorityExtensions);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            // Scalar properties must be freely modifiable
            var config = new AppConfig();

            config.EncryptionKey = "NewKey";
            config.BusinessSoftwareName = "calc.exe";
            config.LogFormat = LogFormat.Xml;
            config.LogMode = LogMode.Centralized;
            config.LargeFileThresholdKo = 2048;

            Assert.Equal("NewKey", config.EncryptionKey);
            Assert.Equal("calc.exe", config.BusinessSoftwareName);
            Assert.Equal(LogFormat.Xml, config.LogFormat);
            Assert.Equal(LogMode.Centralized, config.LogMode);
            Assert.Equal(2048, config.LargeFileThresholdKo);
        }

        [Fact]
        public void Jobs_CanBeAdded()
        {
            // The Jobs collection (unlimited in v3) accepts adding jobs
            var config = new AppConfig();
            config.Jobs.Add(new BackupJob("Test", @"C:\Src", @"D:\Dst", EasySave.Core.Models.Enums.BackupType.Full));

            Assert.Single(config.Jobs);
            Assert.Equal("Test", config.Jobs[0].Name);
        }

        [Fact]
        public void Jobs_CanContainManyEntries()
        {
            // In v3, the number of jobs is unlimited: verification with > 5 (old v1 limit)
            var config = new AppConfig();
            for (int i = 1; i <= 10; i++)
                config.Jobs.Add(new BackupJob($"Job{i}", @"C:\Src", @"D:\Dst", EasySave.Core.Models.Enums.BackupType.Full));

            Assert.Equal(10, config.Jobs.Count);
        }
    }
}