using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Services
{
    // ===== PRIORITY FILE COORDINATOR =====
    public class PriorityFileCoordinator
    {
        private static readonly Lazy<PriorityFileCoordinator> _instance =
            new Lazy<PriorityFileCoordinator>(() => new PriorityFileCoordinator());

        public static PriorityFileCoordinator Instance => _instance.Value;

        private readonly object _lock = new object();
        private readonly Dictionary<string, bool> _priorityPhaseDoneByJob = new Dictionary<string, bool>();
        private readonly ManualResetEventSlim _allPriorityDoneEvent = new ManualResetEventSlim(false);

        private PriorityFileCoordinator() { }

        public void RegisterJob(string jobName)
        {
            if (string.IsNullOrEmpty(jobName)) return;
            lock (_lock)
            {
                _priorityPhaseDoneByJob[jobName] = false;
                _allPriorityDoneEvent.Reset();
            }
        }

        public void NotifyPriorityPhaseDone(string jobName)
        {
            if (string.IsNullOrEmpty(jobName)) return;
            lock (_lock)
            {
                _priorityPhaseDoneByJob[jobName] = true;
                if (AllPriorityPhasesDone())
                    _allPriorityDoneEvent.Set();
            }
        }

        public void UnregisterJob(string jobName)
        {
            if (string.IsNullOrEmpty(jobName)) return;
            lock (_lock)
            {
                _priorityPhaseDoneByJob.Remove(jobName);
                if (AllPriorityPhasesDone())
                    _allPriorityDoneEvent.Set();
            }
        }

        public Task WaitUntilCanTransferNonPriorityAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => _allPriorityDoneEvent.Wait(cancellationToken), cancellationToken);
        }

        private bool AllPriorityPhasesDone()
        {
            if (_priorityPhaseDoneByJob.Count == 0) return true;
            foreach (var done in _priorityPhaseDoneByJob.Values)
                if (!done) return false;
            return true;
        }
    }
}
