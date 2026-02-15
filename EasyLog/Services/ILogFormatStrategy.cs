using EasyLog.Models;
using System.Collections.Generic;
using System.IO;

namespace EasyLog.Services
{
    public interface ILogFormatStrategy
    {
        void WriteEntry(StreamWriter writer, ModelLogEntry entry);
        void WriteEntries(string filePath, List<ModelLogEntry> entries);
        string GetFileExtension();
    }
}
