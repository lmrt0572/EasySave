using EasyLog.Models;

namespace EasyLog.Services
{
    // ===== LOG SERVICE INTERFACE =====
    public interface ILogService
    {
        void Write(ModelLogEntry entry);

        void Flush();
        void SetLogFormat(LogFormat format);
    }
}
