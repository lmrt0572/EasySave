using System;
using System.Text.Json;
using System;
using System.IO;
using System.Text.Json;
using EasyLog.Models;

namespace EasyLog.Services
{
    public class LogService : ILogService
    {
         // ===== SINGLETON =====
        private static readonly Lazy<LogService> _instance = new Lazy<LogService>(() => new LogService());

        public static LogService Instance => _instance.Value;

        // ===== PRIVATE MEMBERS =====
        private readonly string _logDirectory;
        private StreamWritter? _currentWritter;
        private string? _currentLogFile;
        private readonly object _lockObject = new object();

        // ===== CONSTRUCTOR =====
        private LogService()
        {
           string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
           _logDirectory = Path.Combine(appDataPath, "EasySave", "Logs");
           if (!Directory.Exists(_logDirectory))
           {
            Directory.CreateDirectory(_logDirectory);
           }
        }

        // ===== WRITER =====
        public void Write(ModelLogEntry entry)
        {
            
            lock (_lockObject)
                {
                    string today = DateTime.Now.ToString("yyyy-MM-dd");
                    string todayFile = Path.Combine(_logDirectory, $"{today}.json");
                    if (_currentLogFile != todayFile)
                    {
                        _currentWritter?.Close();
                        _currentWritter = new StreamWriter(todayFile, append: true);
                        _currentLogFile = todayFile;
                    }
                    string json = JsonSerializer.Serialize(entry);
                    _currentWritter.WriteLine(json);
                }
        }

        // ===== FLUSH =====
        public void Flush()
        {
            lock (_lockObject)
            {
                _currentWritter?.Flush();
            }
        }
    }
}

