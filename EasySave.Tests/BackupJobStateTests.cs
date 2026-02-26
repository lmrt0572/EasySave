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
        public void Constructor_Default_SetsPausedStatus()
        {
            // Constructor with no arguments should initialize Status to Paused
            var state = new BackupJobState();

            Assert.Equal(BackupStatus.Paused, state.Status);
            Assert.Equal(string.Empty, state.JobName);
        }

        [Fact]
        public void Constructor_WithName_SetsJobName()
        {
            // Constructor with name should set JobName and keep Status = Paused
            var state = new BackupJobState("TestJob");

            Assert.Equal("TestJob", state.JobName);
            Assert.Equal(BackupStatus.Paused, state.Status);
        }

        // ==========================================
        // ===== GetProgression TESTS =====
        // ==========================================

        [Fact]
        public void GetProgression_NoFiles_ReturnsZero()
        {
            // No files: division by zero avoided, returns 0
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
            // 5 files copied out of 10 → 50%
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
            // All files copied → 100%
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
            // 1 copied out of 3 ≈ 33.33%
            var state = new BackupJobState
            {
                TotalFilesToCopy = 3,
                RemainingFiles = 2
            };

            double expected = (1 * 100.0) / 3;
            Assert.Equal(expected, state.GetProgression(), precision: 2);
        }

        // ==========================================
        // ===== StartBackup TESTS =====
        // ==========================================

        [Fact]
        public void StartBackup_SetsRunningState()
        {
            // StartBackup should set Status to Running (no more Active since v3)
            var state = new BackupJobState("Job1");

            state.StartBackup(10, 5000);

            Assert.Equal(BackupStatus.Running, state.Status);
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
        public void CompleteFile_DecrementsRemainingFilesAndSize()
        {
            // Each call to CompleteFile decrements the remaining files/size counters
            var state = new BackupJobState("Job1");
            state.StartBackup(5, 10000);

            state.CompleteFile(2000);

            Assert.Equal(4, state.RemainingFiles);
            Assert.Equal(8000, state.RemainingFilesSize);
        }

        [Fact]
        public void CompleteFile_UpdatesProgressionInt()
        {
            // The Progression property (integer) is recalculated after each file
            var state = new BackupJobState("Job1");
            state.StartBackup(4, 4000);

            state.CompleteFile(1000); // 1/4 = 25 %

            Assert.Equal(25, state.Progression);
        }

        [Fact]
        public void CompleteFile_AllFiles_ProgressionIs100()
        {
            // After all files, Progression == 100 and counters are zero
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
        public void UpdateCurrentFile_SetsSourceAndDestination()
        {
            // UpdateCurrentFile updates the source and destination paths of the current file
            var state = new BackupJobState("Job1");

            state.UpdateCurrentFile(@"C:\source\file.txt", @"D:\target\file.txt");

            Assert.Equal(@"C:\source\file.txt", state.CurrentSourceFile);
            Assert.Equal(@"D:\target\file.txt", state.CurrentDestinationFile);
        }

        // ==========================================
        // ===== Finish TESTS =====
        // ==========================================

        [Fact]
        public void Finish_SetsCompletedStatusAndResetCounters()
        {
            // Finish marks the backup as completed with Progression = 100
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
            // On transfer error, the status should be set to Error
            var state = new BackupJobState("Job1");
            state.StartBackup(5, 5000);

            state.SetError();

            Assert.Equal(BackupStatus.Error, state.Status);
        }

        [Fact]
        public void SetStopped_SetsStoppedStatus()
        {
            // When the user stops a job, the status should be set to Stopped
            var state = new BackupJobState("Job1");
            state.StartBackup(5, 5000);

            state.SetStopped();

            Assert.Equal(BackupStatus.Stopped, state.Status);
        }
    }
}