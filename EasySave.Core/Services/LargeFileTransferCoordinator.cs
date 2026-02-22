using System;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Core.Services
{
    // ===== LARGE FILE TRANSFER COORDINATOR =====
    // Ensures at most ONE file larger than n Ko is transferred at a time across ALL jobs.
    // Small files can transfer in parallel without restriction.
    public class LargeFileTransferCoordinator
    {
        private static readonly Lazy<LargeFileTransferCoordinator> _instance =
            new Lazy<LargeFileTransferCoordinator>(() => new LargeFileTransferCoordinator());

        public static LargeFileTransferCoordinator Instance => _instance.Value;

        private readonly SemaphoreSlim _largeFileSemaphore = new SemaphoreSlim(1, 1);

        private LargeFileTransferCoordinator() { }

        public async Task AcquireSlotIfLargeAsync(long fileSizeBytes, int thresholdKo, CancellationToken cancellationToken)
        {
            if (thresholdKo <= 0) return;
            long thresholdBytes = (long)thresholdKo * 1024;
            if (fileSizeBytes <= thresholdBytes) return;
            await _largeFileSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void ReleaseSlotIfLarge(long fileSizeBytes, int thresholdKo)
        {
            if (thresholdKo <= 0) return;
            long thresholdBytes = (long)thresholdKo * 1024;
            if (fileSizeBytes <= thresholdBytes) return;
            _largeFileSemaphore.Release();
        }
    }
}
