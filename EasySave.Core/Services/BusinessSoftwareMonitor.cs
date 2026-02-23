using System;
using System.Diagnostics;
using System.Threading;

namespace EasySave.Core.Services
{
    // ===== BUSINESS SOFTWARE MONITOR =====
    public class BusinessSoftwareMonitor : IDisposable
    {
        // ===== PRIVATE MEMBERS =====
        private string _processName = string.Empty;
        private Timer? _pollingTimer;
        private bool _isRunning;
        private readonly object _lock = new object();

        // ===== EVENTS =====

        public event Action<bool>? DetectionChanged;

        // ===== PROPERTIES =====
        public bool IsBusinessSoftwareRunning { get; private set; }
        public string ProcessName
        {
            get => _processName;
            set
            {
                _processName = value?.Trim() ?? string.Empty;
                if (_isRunning) CheckProcess();
            }
        }

        // ===== SINGLETON =====
        private static readonly Lazy<BusinessSoftwareMonitor> _instance =
            new Lazy<BusinessSoftwareMonitor>(() => new BusinessSoftwareMonitor());

        public static BusinessSoftwareMonitor Instance => _instance.Value;

        private BusinessSoftwareMonitor() { }

        // ===== START / STOP MONITORING =====
        public void Start(int intervalMs = 2000)
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;

                _pollingTimer = new Timer(_ => CheckProcess(), null, 0, intervalMs);
            }
        }

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
                string name = _processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? _processName[..^4]
                    : _processName;

                var processes = Process.GetProcessesByName(name);
                bool detected = processes.Length > 0;

                foreach (var p in processes) p.Dispose();

                UpdateState(detected);
            }
            catch {}

            }
        }

        private void UpdateState(bool detected)
        {
            bool previousState = IsBusinessSoftwareRunning;
            IsBusinessSoftwareRunning = detected;

            if (detected != previousState)
            {
                DetectionChanged?.Invoke(detected);
            }
        }

        // ===== ONE-SHOT CHECK =====
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
