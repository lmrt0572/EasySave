using System;
using System.Diagnostics;
using System.Threading;

namespace EasySave.Core.Services
{
    // ===== BUSINESS SOFTWARE MONITOR =====
    // V2.0 - Detects running business software to block/stop backups
    public class BusinessSoftwareMonitor : IDisposable
    {
        // ===== PRIVATE MEMBERS =====
        private string _processName = string.Empty;
        private Timer? _pollingTimer;
        private bool _isRunning;
        private readonly object _lock = new object();

        // ===== EVENTS =====
        /// <summary>Fired when business software detection state changes (true = detected)</summary>
        public event Action<bool>? DetectionChanged;

        // ===== PROPERTIES =====
        /// <summary>Current detection state: true if business software is running</summary>
        public bool IsBusinessSoftwareRunning { get; private set; }

        /// <summary>Name of the monitored process (without .exe)</summary>
        public string ProcessName
        {
            get => _processName;
            set
            {
                _processName = value?.Trim() ?? string.Empty;
                // Re-check immediately when process name changes
                if (_isRunning) CheckProcess();
            }
        }

        // ===== SINGLETON =====
        private static readonly Lazy<BusinessSoftwareMonitor> _instance =
            new Lazy<BusinessSoftwareMonitor>(() => new BusinessSoftwareMonitor());

        public static BusinessSoftwareMonitor Instance => _instance.Value;

        private BusinessSoftwareMonitor() { }

        // ===== START / STOP MONITORING =====
        /// <summary>Start polling for the business software process every intervalMs</summary>
        public void Start(int intervalMs = 2000)
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;

                _pollingTimer = new Timer(_ => CheckProcess(), null, 0, intervalMs);
            }
        }

        /// <summary>Stop monitoring</summary>
        public void Stop()
        {
            lock (_lock)
            {
                _isRunning = false;
                _pollingTimer?.Dispose();
                _pollingTimer = null;
            }
        }

        // ===== PROCESS CHECK =====
        private void CheckProcess()
        {
            if (string.IsNullOrWhiteSpace(_processName))
            {
                UpdateState(false);
                return;
            }

            try
            {
                // Remove .exe extension if user provided it
                string name = _processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? _processName[..^4]
                    : _processName;

                var processes = Process.GetProcessesByName(name);
                bool detected = processes.Length > 0;

                // Dispose process handles
                foreach (var p in processes) p.Dispose();

                UpdateState(detected);
            }
            catch
            {
                // Silently handle process enumeration errors
            }
        }

        private void UpdateState(bool detected)
        {
            bool previousState = IsBusinessSoftwareRunning;
            IsBusinessSoftwareRunning = detected;

            // Fire event only on state change
            if (detected != previousState)
            {
                DetectionChanged?.Invoke(detected);
            }
        }

        // ===== ONE-SHOT CHECK =====
        /// <summary>Check once if business software is running (no polling needed)</summary>
        public bool CheckNow()
        {
            CheckProcess();
            return IsBusinessSoftwareRunning;
        }

        // ===== DISPOSE =====
        public void Dispose()
        {
            Stop();
        }
    }
}
