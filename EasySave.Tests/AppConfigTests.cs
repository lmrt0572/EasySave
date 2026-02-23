using Xunit;
using EasySave.Core.Models;
using EasyLog.Models;

namespace EasySave.Tests.Models
{
    // ===== APP CONFIG TESTS =====
    public class AppConfigTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
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
            var config = new AppConfig();

            Assert.Contains(".txt", config.EncryptionExtensions);
            Assert.Contains(".md", config.EncryptionExtensions);
            Assert.Contains(".pdf", config.EncryptionExtensions);
            Assert.Equal(3, config.EncryptionExtensions.Count);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
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
            var config = new AppConfig();
            config.Jobs.Add(new BackupJob("Test", @"C:\Src", @"D:\Dst", EasySave.Core.Models.Enums.BackupType.Full));

            Assert.Single(config.Jobs);
            Assert.Equal("Test", config.Jobs[0].Name);
        }
    }
}