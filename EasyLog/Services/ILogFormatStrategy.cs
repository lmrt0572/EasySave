using EasyLog.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyLog.Services
{
    public interface ILogFormatStrategy
    {
        void WriteEntry(StreamWriter writer, ModelLogEntry entry);
        string GetFileExtension();
    }
}
