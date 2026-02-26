using Xunit;
using EasySave.Core.Services;

namespace EasySave.Tests.Services
{
    // ===== LARGE FILE TRANSFER COORDINATOR TESTS =====

    // Coverage: AcquireSlotIfLargeAsync does not block small files,
    //           does not block if threshold = 0 (disabled), actual blocking for large files,
    //           ReleaseSlotIfLarge releases the semaphore correctly.
    //
    // Note: LargeFileTransferCoordinator is a Singleton. Tests use a fresh instance
    //       via reflection to avoid interference between tests.
    public class LargeFileTransferCoordinatorTests
    {
        // Creates an isolated instance (non-singleton) for tests to avoid
        // side effects between test cases.
    
        private static LargeFileTransferCoordinator CreateFreshInstance()
        {
            var ctor = typeof(LargeFileTransferCoordinator)
                .GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, Type.EmptyTypes, null)!;
            return (LargeFileTransferCoordinator)ctor.Invoke(null);
        }

        // ==========================================
        // ===== AcquireSlotIfLargeAsync =====
        // ==========================================

        [Fact]
        public async Task AcquireSlotIfLarge_SmallFile_DoesNotBlock()
        {
            // A file below the threshold must not acquire the semaphore → immediate return
            var coordinator = CreateFreshInstance();
            int thresholdKo = 1000; // 1 Mo
            long smallFileBytes = 500 * 1024; // 500 Ko

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            // If it blocks, the CancellationToken cancels and throws OperationCanceledException
            await coordinator.AcquireSlotIfLargeAsync(smallFileBytes, thresholdKo, cts.Token);
            // No exception = success
        }

        [Fact]
        public async Task AcquireSlotIfLarge_ThresholdZero_DoesNotBlock()
        {
            // Threshold = 0 means the feature is disabled → no blocking
            var coordinator = CreateFreshInstance();
            long largeFileBytes = 10 * 1024 * 1024; // 10 Mo

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await coordinator.AcquireSlotIfLargeAsync(largeFileBytes, 0, cts.Token);
        }

        [Fact]
        public async Task AcquireSlotIfLarge_LargeFile_AcquiresThenReleasesSuccessfully()
        {
            // A large file acquires the slot, then releasing it allows a second one to pass
            var coordinator = CreateFreshInstance();
            int thresholdKo = 100; // 100 Ko
            long bigFileBytes = 200 * 1024; // 200 Ko > seuil

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // First acquire
            await coordinator.AcquireSlotIfLargeAsync(bigFileBytes, thresholdKo, cts.Token);

            // Release
            coordinator.ReleaseSlotIfLarge(bigFileBytes, thresholdKo);

            // Second acquire possible immediately
            await coordinator.AcquireSlotIfLargeAsync(bigFileBytes, thresholdKo, cts.Token);
            coordinator.ReleaseSlotIfLarge(bigFileBytes, thresholdKo);
        }

        [Fact]
        public async Task AcquireSlotIfLarge_TwoLargeFilesSequential_SecondWaitsForFirst()
        {
            // Two large files cannot pass at the same time:
            // the second must wait for the first to be released.
            var coordinator = CreateFreshInstance();
            int thresholdKo = 100;
            long bigFileBytes = 500 * 1024; // 500 Ko

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            // First acquire
            await coordinator.AcquireSlotIfLargeAsync(bigFileBytes, thresholdKo, cts.Token);

            bool secondStarted = false;

            // Launch the second acquire in a parallel task
            var secondTask = Task.Run(async () =>
            {
                await coordinator.AcquireSlotIfLargeAsync(bigFileBytes, thresholdKo, cts.Token);
                secondStarted = true;
                coordinator.ReleaseSlotIfLarge(bigFileBytes, thresholdKo);
            });

            // Give time to the second to block
            await Task.Delay(100);
            Assert.False(secondStarted, "The second transfer must not start before release");

            // Release the first
            coordinator.ReleaseSlotIfLarge(bigFileBytes, thresholdKo);

            // The second must now unblock
            await secondTask;
            Assert.True(secondStarted);
        }

        // ==========================================
        // ===== ReleaseSlotIfLarge =====
        // ==========================================

        [Fact]
        public void ReleaseSlotIfLarge_SmallFile_DoesNotRelease()
        {
            // Calling Release on a small file must not throw an exception (SemaphoreSlim.Release not called)
            var coordinator = CreateFreshInstance();

            // Must not throw SemaphoreFullException
            var ex = Record.Exception(() => coordinator.ReleaseSlotIfLarge(100 * 1024, 1000));
            Assert.Null(ex);
        }

        [Fact]
        public void ReleaseSlotIfLarge_ThresholdZero_DoesNotRelease()
        {
            // Threshold = 0: silent release
            var coordinator = CreateFreshInstance();
            var ex = Record.Exception(() => coordinator.ReleaseSlotIfLarge(5 * 1024 * 1024, 0));
            Assert.Null(ex);
        }
    }
}