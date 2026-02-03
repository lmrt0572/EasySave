using System;
using System.Collections.Generic;
using System.Text;
using EasyLog.Models;

namespace EasyLog.Services
{
    public interface ILogService
    {
        void Write(ModelLogEntry entry);

        void Flush();
    }
}
