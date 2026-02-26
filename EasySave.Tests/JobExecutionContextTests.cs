using Xunit;
using EasySave.Core.Models;

namespace EasySave.Tests.Models
{
    // ===== JOB EXECUTION CONTEXT TESTS =====
    public class JobExecutionContextTests : IDisposable
    {
        private readonly JobExecutionContext _context;

        public JobExecutionContextTests()
        {
            _context = new JobExecutionContext("TestJob");
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        // ==========================================
        // ===== CONSTRUCTOR TESTS =====
        // ==========================================

        [Fact]
        public void Constructor_SetsJobName()
        {
            Assert.Equal("TestJob", _context.JobName);
        }

        [Fact]
        public void Constructor_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new JobExecutionContext(null!));
        }

        [Fact]
        public void Constructor_InitialState_NotPausedNotStopped()
        {
            Assert.False(_context.IsPaused);
            Assert.False(_context.IsStopped);
            Assert.False(_context.PausedByUser);
            Assert.False(_context.PausedByMonitor);
        }

        // ==========================================
        // ===== PAUSE / RESUME (USER) =====
        // ==========================================

        [Fact]
        public void Pause_SetsIsPausedTrue()
        {
            _context.Pause();

            Assert.True(_context.IsPaused);
            Assert.True(_context.PausedByUser);
        }

        [Fact]
        public void Resume_AfterPause_ClearsPause()
        {
            _context.Pause();
            _context.Resume();

            Assert.False(_context.IsPaused);
            Assert.False(_context.PausedByUser);
        }

        // ==========================================
        // ===== PAUSE / RESUME (MONITOR) =====
        // ==========================================

        [Fact]
        public void PauseByMonitor_SetsIsPausedTrue()
        {
            _context.PauseByMonitor();

            Assert.True(_context.IsPaused);
            Assert.True(_context.PausedByMonitor);
        }

        [Fact]
        public void ResumeFromMonitor_AfterMonitorPause_ClearsPause()
        {
            _context.PauseByMonitor();
            _context.ResumeFromMonitor();

            Assert.False(_context.IsPaused);
            Assert.False(_context.PausedByMonitor);
        }

        // ==========================================
        // ===== DOUBLE PAUSE SCENARIO =====
        // ==========================================

        [Fact]
        public void Resume_WhenMonitorStillPaused_StaysPaused()
        {
            // Both user and monitor pause
            _context.Pause();
            _context.PauseByMonitor();

            // User resumes, but monitor is still paused
            _context.Resume();

            Assert.True(_context.IsPaused, "Should stay paused because monitor is still active");
            Assert.False(_context.PausedByUser);
            Assert.True(_context.PausedByMonitor);
        }

        [Fact]
        public void ResumeFromMonitor_WhenUserStillPaused_StaysPaused()
        {
            // Both user and monitor pause
            _context.Pause();
            _context.PauseByMonitor();

            // Monitor resumes, but user still paused
            _context.ResumeFromMonitor();

            Assert.True(_context.IsPaused, "Should stay paused because user pause is still active");
            Assert.True(_context.PausedByUser);
            Assert.False(_context.PausedByMonitor);
        }

        [Fact]
        public void BothResume_FullyResumes()
        {
            _context.Pause();
            _context.PauseByMonitor();

            _context.Resume();
            _context.ResumeFromMonitor();

            Assert.False(_context.IsPaused);
        }

        // ==========================================
        // ===== STOP TESTS =====
        // ==========================================

        [Fact]
        public void Stop_SetsIsStoppedTrue()
        {
            _context.Stop();

            Assert.True(_context.IsStopped);
        }

        [Fact]
        public void Stop_CancelsToken()
        {
            _context.Stop();

            Assert.True(_context.Token.IsCancellationRequested);
        }

        [Fact]
        public void Stop_UnblocksPause()
        {
            _context.Pause();
            Assert.True(_context.IsPaused);

            _context.Stop();

            // After stop, the pause gate should be set (unblocked)
            // so that ThrowIfStoppedOrWaitIfPaused can throw OperationCanceledException
            Assert.False(_context.IsPaused);
        }

        // ==========================================
        // ===== ThrowIfStoppedOrWaitIfPaused =====
        // ==========================================

        [Fact]
        public void ThrowIfStoppedOrWaitIfPaused_NotPausedNotStopped_DoesNotThrow()
        {
            // Should return immediately without exception
            _context.ThrowIfStoppedOrWaitIfPaused();
        }

        [Fact]
        public void ThrowIfStoppedOrWaitIfPaused_Stopped_ThrowsOperationCanceled()
        {
            _context.Stop();

            Assert.Throws<OperationCanceledException>(() => _context.ThrowIfStoppedOrWaitIfPaused());
        }

        // ==========================================
        // ===== LARGE FILE THRESHOLD =====
        // ==========================================

        [Fact]
        public void LargeFileThresholdKo_DefaultsToZero()
        {
            Assert.Equal(0, _context.LargeFileThresholdKo);
        }

        [Fact]
        public void LargeFileThresholdKo_CanBeSet()
        {
            _context.LargeFileThresholdKo = 1024;

            Assert.Equal(1024, _context.LargeFileThresholdKo);
        }
    }
}