using EasyLog.Models;
using System.IO;

namespace EasyLog.Services
{
    public interface ILogFormatStrategy
    {
        void WriteEntry(StreamWriter writer, ModelLogEntry entry);
        string GetFileExtension();
    }
}
