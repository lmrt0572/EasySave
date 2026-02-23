using Xunit;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;

namespace EasySave.Tests.Models
{
    // ===== BACKUP JOB TESTS =====
    public class BackupJobTests
    {
        [Fact]
        public void Constructor_Default_SetsEmptyStringsAndFullType()
        {
            var job = new BackupJob();

            Assert.Equal(string.Empty, job.Name);
            Assert.Equal(string.Empty, job.SourceDirectory);
            Assert.Equal(string.Empty, job.TargetDirectory);
            Assert.Equal(BackupType.Full, job.Type);
        }

        [Fact]
        public void Constructor_WithParameters_SetsAllProperties()
        {
            var job = new BackupJob("MyBackup", @"C:\Source", @"D:\Target", BackupType.Differential);

            Assert.Equal("MyBackup", job.Name);
            Assert.Equal(@"C:\Source", job.SourceDirectory);
            Assert.Equal(@"D:\Target", job.TargetDirectory);
            Assert.Equal(BackupType.Differential, job.Type);
        }

        [Fact]
        public void Properties_CanBeModified()
        {
            var job = new BackupJob();
            job.Name = "Updated";
            job.SourceDirectory = @"C:\NewSource";
            job.TargetDirectory = @"D:\NewTarget";
            job.Type = BackupType.Differential;

            Assert.Equal("Updated", job.Name);
            Assert.Equal(@"C:\NewSource", job.SourceDirectory);
            Assert.Equal(@"D:\NewTarget", job.TargetDirectory);
            Assert.Equal(BackupType.Differential, job.Type);
        }
    }
}