using Xunit;
using EasySave.Core.Services;

namespace EasySave.Tests.Services
{
    // ===== BUSINESS SOFTWARE MONITOR TESTS =====

    // Coverage: CheckNow with nonexistent process, ProcessName management,
    //           .exe normalization, DetectionChanged event,
    //           Timer Start/Stop, Dispose.
    
    // Note: BusinessSoftwareMonitor is a Singleton. Tests use a fresh instance
    //       via reflection to avoid interactions with the global Singleton.
    public class BusinessSoftwareMonitorTests : IDisposable
    {
        private readonly BusinessSoftwareMonitor _monitor;

        public BusinessSoftwareMonitorTests()
        {
            _monitor = CreateFreshInstance();
        }

        public void Dispose()
        {
            _monitor.Dispose();
        }

        private static BusinessSoftwareMonitor CreateFreshInstance()
        {
            var ctor = typeof(BusinessSoftwareMonitor)
                .GetConstructor(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    null, Type.EmptyTypes, null)!;
            return (BusinessSoftwareMonitor)ctor.Invoke(null);
        }

        // ==========================================
        // ===== ProcessName PROPERTY =====
        // ==========================================

        [Fact]
        public void ProcessName_Default_IsEmpty()
        {
            // Without configuration, the process name must be empty
            Assert.Equal(string.Empty, _monitor.ProcessName);
        }

        [Fact]
        public void ProcessName_Set_TrimsWhitespace()
        {
            // Spaces around the name must be removed
            _monitor.ProcessName = "  calc.exe  ";
            Assert.Equal("calc.exe", _monitor.ProcessName);
        }

        [Fact]
        public void ProcessName_SetNull_DefaultsToEmpty()
        {
            // null must be treated as empty string
            _monitor.ProcessName = null!;
            Assert.Equal(string.Empty, _monitor.ProcessName);
        }

        // ==========================================
        // ===== CheckNow =====
        // ==========================================

        [Fact]
        public void CheckNow_EmptyProcessName_ReturnsFalse()
        {
            // Without a configured name, no business process can be detected
            _monitor.ProcessName = string.Empty;
            bool result = _monitor.CheckNow();
            Assert.False(result);
        }

        [Fact]
        public void CheckNow_NonExistentProcess_ReturnsFalse()
        {
            // A process name that does not exist → no detection
            _monitor.ProcessName = "this_process_definitely_does_not_exist_xyz987";
            bool result = _monitor.CheckNow();
            Assert.False(result);
        }

        [Fact]
        public void CheckNow_CurrentProcess_ReturnsTrue()
        {
            // The current process (dotnet/testhost) necessarily exists → positive detection
            string currentProcess = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            _monitor.ProcessName = currentProcess;

            bool result = _monitor.CheckNow();

            Assert.True(result);
        }

        [Fact]
        public void CheckNow_ProcessNameWithExe_NormalizesAndDetects()
        {
            // The name with ".exe" must be normalized (without extension) for GetProcessesByName
            string currentProcess = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            _monitor.ProcessName = currentProcess + ".exe";

            bool result = _monitor.CheckNow();

            Assert.True(result);
        }

        // ==========================================
        // ===== IsBusinessSoftwareRunning =====
        // ==========================================

        [Fact]
        public void IsBusinessSoftwareRunning_Default_IsFalse()
        {
            // By default, no business software is detected
            Assert.False(_monitor.IsBusinessSoftwareRunning);
        }

        [Fact]
        public void IsBusinessSoftwareRunning_AfterCheckNow_ReflectsResult()
        {
            // IsBusinessSoftwareRunning must be updated after CheckNow
            _monitor.ProcessName = "this_process_definitely_does_not_exist_xyz987";
            _monitor.CheckNow();

            Assert.False(_monitor.IsBusinessSoftwareRunning);
        }

        // ==========================================
        // ===== DetectionChanged EVENT =====
        // ==========================================

        [Fact]
        public void DetectionChanged_FiredWhenStateChanges()
        {
            // The DetectionChanged event must be raised when state changes
            bool? eventValue = null;
            _monitor.DetectionChanged += detected => eventValue = detected;

            // Change from false → true
            string currentProcess = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            _monitor.ProcessName = currentProcess;
            _monitor.CheckNow();

            Assert.NotNull(eventValue);
            Assert.True(eventValue);
        }

        [Fact]
        public void DetectionChanged_NotFiredWhenStateUnchanged()
        {
            // The event must NOT be raised if the state does not change
            int eventCount = 0;
            _monitor.DetectionChanged += _ => eventCount++;

            // Two consecutive CheckNow on a nonexistent process → state remains false
            _monitor.ProcessName = "nonexistent_xyz987";
            _monitor.CheckNow();
            int countAfterFirst = eventCount; // may be 0 or 1 depending on initial state
            _monitor.CheckNow();

            // The second call must not increase the counter (state unchanged)
            Assert.Equal(countAfterFirst, eventCount);
        }

        // ==========================================
        // ===== START / STOP =====
        // ==========================================

        [Fact]
        public void Start_ThenStop_DoesNotThrow()
        {
            // The polling timer start/stop cycle must not throw an exception
            var ex = Record.Exception(() =>
            {
                _monitor.Start(500);
                Thread.Sleep(50);
                _monitor.Stop();
            });
            Assert.Null(ex);
        }

        [Fact]
        public void Start_Twice_DoesNotThrow()
        {
            // Calling Start twice is idempotent (internal guard)
            var ex = Record.Exception(() =>
            {
                _monitor.Start(500);
                _monitor.Start(500); // second call ignored
                _monitor.Stop();
            });
            Assert.Null(ex);
        }

        // ==========================================
        // ===== DISPOSE =====
        // ==========================================

        [Fact]
        public void Dispose_AfterStart_DoesNotThrow()
        {
            var monitor = CreateFreshInstance();
            monitor.Start(500);
            var ex = Record.Exception(() => monitor.Dispose());
            Assert.Null(ex);
        }
    }
}