using Xunit;
using EasySave.Core.Models;
using EasySave.Core.Models.Enums;

namespace EasySave.Tests.Models
{
    // ===== BACKUP JOB STATE TESTS =====
    public class BackupJobStateTests
    {
        // ==========================================
        // ===== CONSTRUCTOR TESTS =====
        // ==========================================

        [Fact]
        public void Constructor_Default_SetsInactiveStatus()
        {
            var state = new BackupJobState();

            Assert.Equal(BackupStatus.Inactive, state.Status);
            Assert.Equal(string.Empty, state.JobName);
        }

        [Fact]
        public void Constructor_WithName_SetsJobName()
        {
            var state = new BackupJobState("TestJob");

            Assert.Equal("TestJob", state.JobName);
            Assert.Equal(BackupStatus.Inactive, state.Status);
        }

        // ==========================================
        // ===== GetProgression TESTS =====
        // ==========================================

        [Fact]
        public void GetProgression_NoFiles_ReturnsZero()
        {
            var state = new BackupJobState
            {
                TotalFilesToCopy = 0,
                RemainingFiles = 0
            };

            Assert.Equal(0, state.GetProgression());
        }

        [Fact]
        public void GetProgression_HalfDone_Returns50()
        {
            var state = new BackupJobState
            {
                TotalFilesToCopy = 10,
                RemainingFiles = 5
            };

            Assert.Equal(50.0, state.GetProgression());
        }

        [Fact]
        public void GetProgression_AllDone_Returns100()
        {
            var state = new BackupJobState
            {
                TotalFilesToCopy = 10,
                RemainingFiles = 0
            };

            Assert.Equal(100.0, state.GetProgression());
        }

        [Fact]
        public void GetProgression_OneOfThree_ReturnsCorrectPercentage()
        {
            var state = new BackupJobState
            {
                TotalFilesToCopy = 3,
                RemainingFiles = 2
            };

            double expected = (1 * 100.0) / 3; // ~33.33
            Assert.Equal(expected, state.GetProgression(), precision: 2);
        }

        // ==========================================
        // ===== StartBackup TESTS =====
        // ==========================================

        [Fact]
        public void StartBackup_SetsActiveState()
        {
            var state = new BackupJobState("Job1");

            state.StartBackup(10, 5000);

            Assert.Equal(BackupStatus.Active, state.Status);
            Assert.Equal(10, state.TotalFilesToCopy);
            Assert.Equal(5000, state.TotalFilesSize);
            Assert.Equal(10, state.RemainingFiles);
            Assert.Equal(5000, state.RemainingFilesSize);
            Assert.Equal(0, state.Progression);
        }

        // ==========================================
        // ===== CompleteFile TESTS =====
        // ==========================================

        [Fact]
        public void CompleteFile_DecrementsRemainingFiles()
        {
            var state = new BackupJobState("Job1");
            state.StartBackup(5, 10000);

            state.CompleteFile(2000);

            Assert.Equal(4, state.RemainingFiles);
            Assert.Equal(8000, state.RemainingFilesSize);
        }

        [Fact]
        public void CompleteFile_UpdatesProgression()
        {
            var state = new BackupJobState("Job1");
            state.StartBackup(4, 4000);

            state.CompleteFile(1000); // 1/4 done = 25%

            Assert.Equal(25, state.Progression);
        }

        [Fact]
        public void CompleteFile_AllFiles_ProgressionIs100()
        {
            var state = new BackupJobState("Job1");
            state.StartBackup(2, 2000);

            state.CompleteFile(1000);
            state.CompleteFile(1000);

            Assert.Equal(100, state.Progression);
            Assert.Equal(0, state.RemainingFiles);
            Assert.Equal(0, state.RemainingFilesSize);
        }

        // ==========================================
        // ===== UpdateCurrentFile TESTS =====
        // ==========================================

        [Fact]
        public void UpdateCurrentFile_SetsSourceAndDest()
        {
            var state = new BackupJobState("Job1");

            state.UpdateCurrentFile(@"C:\source\file.txt", @"D:\target\file.txt");

            Assert.Equal(@"C:\source\file.txt", state.CurrentSourceFile);
            Assert.Equal(@"D:\target\file.txt", state.CurrentDestinationFile);
        }

        // ==========================================
        // ===== Finish TESTS =====
        // ==========================================

        [Fact]
        public void Finish_SetsCompletedState()
        {
            var state = new BackupJobState("Job1");
            state.StartBackup(5, 5000);

            state.Finish();

            Assert.Equal(BackupStatus.Completed, state.Status);
            Assert.Equal(100, state.Progression);
            Assert.Equal(0, state.RemainingFiles);
            Assert.Equal(0, state.RemainingFilesSize);
        }

        // ==========================================
        // ===== SetError / SetStopped TESTS =====
        // ==========================================

        [Fact]
        public void SetError_SetsErrorStatus()
        {
            var state = new BackupJobState("Job1");
            state.StartBackup(5, 5000);

            state.SetError();

            Assert.Equal(BackupStatus.Error, state.Status);
        }

        [Fact]
        public void SetStopped_SetsStoppedStatus()
        {
            var state = new BackupJobState("Job1");
            state.StartBackup(5, 5000);

            state.SetStopped();

            Assert.Equal(BackupStatus.Stopped, state.Status);
        }
    }
}