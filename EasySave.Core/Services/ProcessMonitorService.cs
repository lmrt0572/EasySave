using System;
using System.Diagnostics;
using System.Linq;

namespace EasySave.Core.Services
{
    /// <summary>
    /// Service de monitoring des processus Windows (Tickets #12, #13, #14, #15)
    /// </summary>
    public class ProcessMonitorService
    {
        private string _businessSoftwareName = string.Empty;

        public event EventHandler<BusinessSoftwareDetectedEventArgs>? BusinessSoftwareDetected;

        public void ConfigureBusinessSoftware(string processName)
        {
            _businessSoftwareName = processName ?? string.Empty;
        }

        public string GetBusinessSoftwareName() => _businessSoftwareName;

        public bool IsBusinessSoftwareRunning()
        {
            if (string.IsNullOrWhiteSpace(_businessSoftwareName))
                return false;

            try
            {
                var processes = Process.GetProcesses();
                return processes.Any(p =>
                {
                    try
                    {
                        return p.ProcessName.Equals(_businessSoftwareName, StringComparison.OrdinalIgnoreCase);
                    }
                    catch { return false; }
                });
            }
            catch { return false; }
        }

        // Pour la V3 : polling périodique
        public void StartMonitoring() { /* Implémenter plus tard */ }
        public void StopMonitoring() { /* Implémenter plus tard */ }
    }

    public class BusinessSoftwareDetectedEventArgs : EventArgs
    {
        public string ProcessName { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
    }
}