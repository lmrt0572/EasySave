using System;
using System.Threading;

namespace EasySave.Core.Models
{
    // ===== JOB EXECUTION CONTEXT =====
    // V3.0 - Per-job synchronization: pause, resume, stop
    // Wraps ManualResetEventSlim (pause gate) + CancellationTokenSource (stop)
    public class JobExecutionContext : IDisposable
    {
        // ===== PRIVATE FIELDS =====
        private readonly ManualResetEventSlim _pauseGate = new ManualResetEventSlim(true); // starts signaled (not paused)
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        // ===== PROPERTIES =====

   
        public string JobName { get; }

        public bool PausedByUser { get; private set; }

        public bool PausedByMonitor { get; set; }

        public bool IsStopped => _cts.IsCancellationRequested;

        public bool IsPaused => !_pauseGate.IsSet;

        public CancellationToken Token => _cts.Token;

        // ===== CONSTRUCTOR =====
        public JobExecutionContext(string jobName)
        {
            JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
        }

        // ===== PAUSE / RESUME / STOP =====

        public void Pause()
        {
            PausedByUser = true;
            _pauseGate.Reset();
        }

        public void PauseByMonitor()
        {
            PausedByMonitor = true;
            _pauseGate.Reset();
        }

        public void Resume()
        {
            PausedByUser = false;

            if (!PausedByMonitor)
                _pauseGate.Set();
        }

        public void ResumeFromMonitor()
        {
            PausedByMonitor = false;
            if (!PausedByUser)
                _pauseGate.Set();
        }

        public void Stop()
        {
            _cts.Cancel();
            _pauseGate.Set();
        }

        // ===== SYNCHRONIZATION CHECKPOINT =====

        public void ThrowIfStoppedOrWaitIfPaused()
        {
            _cts.Token.ThrowIfCancellationRequested();
            _pauseGate.Wait(_cts.Token);
        }

        // ===== DISPOSE =====
        public void Dispose()
        {
            _pauseGate.Dispose();
            _cts.Dispose();
        }
    }
}