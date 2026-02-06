using System;
using System.Text.Json;
using System.IO;
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
        private StreamWriter? _currentWriter;
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
                    _currentWriter?.Close();
                    _currentWriter = new StreamWriter(todayFile, append: true);
                    _currentLogFile = todayFile;
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(entry, options);

                _currentWriter.WriteLine(json);      // écrit le bloc JSON indenté
                _currentWriter.WriteLine();          // ligne vide entre deux entrées (facultatif)
            }
        }


        // ===== FLUSH =====
        public void Flush()
        {
            lock (_lockObject)
            {
                _currentWriter?.Flush();
            }
        }
    }
}
