using Xunit;
using EasySave.Core.Services;

namespace EasySave.Tests.Services
{
    // ===== PRIORITY FILE COORDINATOR TESTS =====

    // Coverage : RegisterJob, NotifyPriorityPhaseDone, UnregisterJob,
    //            WaitUntilCanTransferNonPriorityAsync, empty job dictionary,
    //            unblocking when all jobs have completed their priority phase.
    //
    // Note : PriorityFileCoordinator is a Singleton. Tests use a fresh instance
    //        via reflection to avoid interference between test cases.
    public class PriorityFileCoordinatorTests
    {
        private static PriorityFileCoordinator CreateFreshInstance()
        {
            var ctor = typeof(PriorityFileCoordinator)
                .GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, Type.EmptyTypes, null)!;
            return (PriorityFileCoordinator)ctor.Invoke(null);
        }

        // ==========================================
        // ===== RegisterJob =====
        // ==========================================

        [Fact]
        public void RegisterJob_ValidName_DoesNotThrow()
        {
            // Registering a valid job name must not throw any exception
            var coordinator = CreateFreshInstance();
            var ex = Record.Exception(() => coordinator.RegisterJob("Job1"));
            Assert.Null(ex);
        }

        [Fact]
        public void RegisterJob_NullOrEmpty_IsIgnored()
        {
            // Null or empty names must be silently ignored
            var coordinator = CreateFreshInstance();
            var ex = Record.Exception(() =>
            {
                coordinator.RegisterJob(null!);
                coordinator.RegisterJob(string.Empty);
            });
            Assert.Null(ex);
        }

        // ==========================================
        // ===== WaitUntilCanTransferNonPriority =====
        // ==========================================

        [Fact]
        public async Task Wait_NoJobsRegistered_ReturnsImmediately()
        {
            // With no registered jobs, AllPriorityPhasesDone() returns true.
            // The ManualResetEventSlim starts as false (not set), so we must first
            // bring the coordinator into the "all done" state by registering then
            // immediately unregistering a job — the internal event is then Set.
            var coordinator = CreateFreshInstance();

            // Register then unregister: dictionary becomes empty → event is Set
            coordinator.RegisterJob("Temp");
            coordinator.UnregisterJob("Temp");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            // Must return without blocking because the dictionary is empty
            await coordinator.WaitUntilCanTransferNonPriorityAsync(cts.Token);
        }

        [Fact]
        public async Task Wait_SingleJob_BlocksUntilNotified()
        {
            // A registered but not yet notified job must block non-priority transfers
            var coordinator = CreateFreshInstance();
            coordinator.RegisterJob("Job1");

            bool unblocked = false;

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var waitTask = Task.Run(async () =>
            {
                await coordinator.WaitUntilCanTransferNonPriorityAsync(cts.Token);
                unblocked = true;
            });

            // Give the wait task time to block
            await Task.Delay(100);
            Assert.False(unblocked, "Must stay blocked while priority phase is not done");

            coordinator.NotifyPriorityPhaseDone("Job1");
            await waitTask;
            Assert.True(unblocked);
        }

        [Fact]
        public async Task Wait_TwoJobs_BlocksUntilBothNotified()
        {
            // With two jobs, the unblock must only happen after BOTH have notified
            var coordinator = CreateFreshInstance();
            coordinator.RegisterJob("JobA");
            coordinator.RegisterJob("JobB");

            bool unblocked = false;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            var waitTask = Task.Run(async () =>
            {
                await coordinator.WaitUntilCanTransferNonPriorityAsync(cts.Token);
                unblocked = true;
            });

            await Task.Delay(100);
            Assert.False(unblocked);

            // First job done — must still be blocked
            coordinator.NotifyPriorityPhaseDone("JobA");
            await Task.Delay(100);
            Assert.False(unblocked, "One job done is not enough to unblock");

            // Second job done — must now unblock
            coordinator.NotifyPriorityPhaseDone("JobB");
            await waitTask;
            Assert.True(unblocked);
        }

        // ==========================================
        // ===== UnregisterJob =====
        // ==========================================

        [Fact]
        public async Task Unregister_LastJob_UnblocksWaiters()
        {
            // Unregistering the last active job must unblock waiting tasks
            var coordinator = CreateFreshInstance();
            coordinator.RegisterJob("Solo");

            bool unblocked = false;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var waitTask = Task.Run(async () =>
            {
                await coordinator.WaitUntilCanTransferNonPriorityAsync(cts.Token);
                unblocked = true;
            });

            await Task.Delay(100);
            Assert.False(unblocked);

            coordinator.UnregisterJob("Solo");
            await waitTask;
            Assert.True(unblocked);
        }

        [Fact]
        public void UnregisterJob_NullOrEmpty_IsIgnored()
        {
            // Null or empty names must be silently ignored on unregister too
            var coordinator = CreateFreshInstance();
            var ex = Record.Exception(() =>
            {
                coordinator.UnregisterJob(null!);
                coordinator.UnregisterJob(string.Empty);
            });
            Assert.Null(ex);
        }

        // ==========================================
        // ===== NotifyPriorityPhaseDone =====
        // ==========================================

        [Fact]
        public void NotifyPriorityPhaseDone_UnregisteredJob_DoesNotThrow()
        {
            // Notifying an unregistered job must not throw
            var coordinator = CreateFreshInstance();
            var ex = Record.Exception(() => coordinator.NotifyPriorityPhaseDone("Ghost"));
            Assert.Null(ex);
        }

        [Fact]
        public async Task NotifyTwice_SameJob_StillUnblocks()
        {
            // Double notification for the same job must be idempotent and not deadlock
            var coordinator = CreateFreshInstance();
            coordinator.RegisterJob("Dup");

            coordinator.NotifyPriorityPhaseDone("Dup");
            coordinator.NotifyPriorityPhaseDone("Dup");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await coordinator.WaitUntilCanTransferNonPriorityAsync(cts.Token);
        }
    }
}