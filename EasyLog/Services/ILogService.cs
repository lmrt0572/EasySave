using EasyLog.Models;

namespace EasyLog.Services
{
    public interface ILogService
    {
        void Write(ModelLogEntry entry);

        void Flush();
    }
}
