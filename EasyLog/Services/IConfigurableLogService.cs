using System;
using System.Collections.Generic;
using System.Text;
using EasyLog.Services;

namespace EasyLog.Services
{
    public interface IConfigurableLogService
    {
        void SetFormat(string format); // "json" ou "xml"
        string GetFormat();
    }
}
