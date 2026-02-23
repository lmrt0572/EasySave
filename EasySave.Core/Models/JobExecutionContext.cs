using System;
using System.Collections.Generic;
using System.Threading;
    // ===== JOB EXECUTION CONTEXT =====
    // Per-job synchronization: pause, resume, stop
    // Wraps ManualResetEventSlim (pause gate) + CancellationTokenSource (stop)

namespace EasySave.Core.Models
{
        // ===== PROPERTIES =====

   
    public class JobExecutionContext : IDisposable
    {
        private readonly ManualResetEventSlim _pauseGate = new ManualResetEventSlim(true);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public string JobName { get; }

        public bool PausedByUser { get; private set; }

        public bool PausedByMonitor { get; set; }

        public bool IsStopped => _cts.IsCancellationRequested;

        public bool IsPaused => !_pauseGate.IsSet;

        public CancellationToken Token => _cts.Token;

        public int LargeFileThresholdKo { get; set; }
        // ===== CONSTRUCTOR =====

        public IReadOnlyList<string> PriorityExtensions { get; set; } = Array.Empty<string>();

        public JobExecutionContext(string jobName)
        {
        // ===== PAUSE / RESUME / STOP =====

            JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
        }

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

        // ===== SYNCHRONIZATION CHECKPOINT =====

        public void Stop()
        {
            _cts.Cancel();
            _pauseGate.Set();
        }

        // ===== SYNCHRONIZATION CHECKPOINT =====
        // ===== DISPOSE =====
        public void ThrowIfStoppedOrWaitIfPaused()
        {
            _cts.Token.ThrowIfCancellationRequested();
            _pauseGate.Wait(_cts.Token);
        }

        public void Dispose()
        {
            _pauseGate.Dispose();
            _cts.Dispose();
        }
    }
}