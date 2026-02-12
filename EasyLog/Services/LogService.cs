using EasyLog.Models;
using EasyLog.Services.Strategies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        private ILogFormatStrategy _currentStrategy;


        // ===== CONSTRUCTOR =====
        private LogService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appDataPath, "EasySave", "Logs");
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // Default strategy
            _currentStrategy = new JsonLogStrategy();
        }

        // ===== STRATEGY SETTER =====
        public void SetLogFormat(LogFormat format)
        {
            lock (_lockObject)
            {
                // Close the current file
                _currentWriter?.Close();
                _currentWriter = null;

                // Strategy changing
                _currentStrategy = format == LogFormat.Json
                    ? new JsonLogStrategy()
                    : new XmlLogStrategy();

                // Migrate existing logs
                MigrateLogsToNewFormat(format);

                _currentLogFile = null;
            }
        }
        // ===== MIGRATION =====

        private void MigrateLogsToNewFormat(LogFormat newFormat)
        {
            string sourceExtension = newFormat == LogFormat.Json ? ".xml" : ".json";
            string targetExtension = newFormat == LogFormat.Json ? ".json" : ".xml";

            string[] sourceFiles;
            try
            {
                sourceFiles = Directory.GetFiles(_logDirectory, "*" + sourceExtension);
            }
            catch
            {
                return;
            }

            foreach (var oldFile in sourceFiles)
            {
                string newFile = Path.ChangeExtension(oldFile, targetExtension.TrimStart('.'));

                // Reading old entries
                var entries = ReadEntriesFromFile(oldFile, sourceExtension);

                // If nothing could be read, skip this file
                if (entries.Count == 0)
                    continue;

                // Writing in new format
                try
                {
                    using (var writer = new StreamWriter(newFile, append: false))
                    {
                        foreach (var entry in entries)
                        {
                            _currentStrategy.WriteEntry(writer, entry);
                        }
                    }

                    File.Delete(oldFile);
                }
                catch
                {
                    // Ignore migration errors on a per-file basis
                }
            }
        }

        // ===== INPUTS READING =====
        private List<ModelLogEntry> ReadEntriesFromFile(string filePath, string extension)
        {
            var entries = new List<ModelLogEntry>();

            if (extension == ".json")
            {
                string content;
                try
                {
                    content = File.ReadAllText(filePath);
                }
                catch
                {
                    return entries;
                }

                var lines = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    try
                    {
                        var entry = System.Text.Json.JsonSerializer.Deserialize<ModelLogEntry>(line.Trim());
                        if (entry != null)
                            entries.Add(entry);
                    }
                    catch
                    {
                        // Ignore malformed JSON entries
                    }
                }
            }
            else // XML
            {
                try
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        try
                        {
                            var rootSerializer = new System.Xml.Serialization.XmlSerializer(typeof(LogEntries));
                            var root = (LogEntries?)rootSerializer.Deserialize(reader);
                            if (root != null && root.Entries != null)
                            {
                                entries.AddRange(root.Entries);
                                return entries;
                            }
                        }
                        catch
                        {
                            reader.BaseStream.Position = 0;
                            reader.DiscardBufferedData();
                        }

                        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(ModelLogEntry));
                        string content = reader.ReadToEnd();

                        if (content.Contains("<?xml"))
                        {
                            var xmlDocs = content.Split(new[] { "<?xml" }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var xmlDoc in xmlDocs)
                            {
                                try
                                {
                                    string fullXml = "<?xml" + xmlDoc.Trim();
                                    using (var stringReader = new StringReader(fullXml))
                                    {
                                        var entry = (ModelLogEntry?)serializer.Deserialize(stringReader);
                                        if (entry != null)
                                            entries.Add(entry);
                                    }
                                }
                                catch
                                {
                                    // Ignore malformed XML entries
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors while reading XML log file
                }
            }

            return entries;

        }

        // ===== WRITER =====
        public void Write(ModelLogEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            lock (_lockObject)
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string extension = _currentStrategy.GetFileExtension();
                string todayFile = Path.Combine(_logDirectory, $"{today}{extension}");

                if (extension == ".xml")
                {
                    try
                    {
                        WriteXmlEntry(todayFile, entry);
                    }
                    catch
                    {
                        // Ignore XML logging errors
                    }
                    return;
                }

                if (_currentLogFile != todayFile)
                {
                    try
                    {
                        _currentWriter?.Close();
                        _currentWriter = new StreamWriter(todayFile, append: true);
                        _currentLogFile = todayFile;
                    }
                    catch
                    {
                        _currentWriter = null;
                        _currentLogFile = null;
                        return;
                    }
                }

                try
                {
                    _currentStrategy.WriteEntry(_currentWriter!, entry);
                }
                catch
                {
                    // Ignore logging errors
                }
            }
        }

        // ===== XML HELPER =====
        private void WriteXmlEntry(string filePath, ModelLogEntry entry)
        {
            LogEntries root;

            if (File.Exists(filePath))
            {
                try
                {
                    using (var reader = new StreamReader(filePath))
                    {
                        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(LogEntries));
                        root = (LogEntries?)serializer.Deserialize(reader) ?? new LogEntries();
                    }
                }
                catch
                {
                    root = new LogEntries();
                }
            }
            else
            {
                root = new LogEntries();
            }

            root.Entries.Add(entry);

            var settings = new System.Xml.XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = false
            };

            using (var writer = System.Xml.XmlWriter.Create(filePath, settings))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(LogEntries));
                serializer.Serialize(writer, root);
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
