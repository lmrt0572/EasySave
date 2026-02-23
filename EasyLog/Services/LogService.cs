using EasyLog.Models;
using EasyLog.Services.Strategies;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;

namespace EasyLog.Services
{
    public class LogService : ILogService
    {
        private const int DebounceMs = 300;

        // ===== SINGLETON =====
        private static readonly Lazy<LogService> _instance = new Lazy<LogService>(() => new LogService());
        public static LogService Instance => _instance.Value;

        // ===== PRIVATE MEMBERS =====
        private readonly string _logDirectory;
        private readonly object _ioLock = new object();
        private ILogFormatStrategy _currentStrategy;

        private readonly ConcurrentQueue<ModelLogEntry> _pendingEntries = new();
        private readonly Timer? _debounceTimer;
        private volatile int _writePending;

        private LogMode _currentMode = LogMode.Both;
        private static readonly HttpClient _httpClient = new HttpClient();
        private string DockerUrl = "http://localhost:8080/api/logs";

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
            _debounceTimer = new Timer(_ => FlushDebounced(), null, Timeout.Infinite, Timeout.Infinite);
        }

        // ===== STRATEGY SETTER =====
        public void SetLogFormat(LogFormat format)
        {
            Flush();
            lock (_ioLock)
            {
                // Strategy changing
                _currentStrategy = format == LogFormat.Json ? new JsonLogStrategy() : new XmlLogStrategy();

                // Migrate existing logs
                MigrateLogsToNewFormat(format);
            }
        }

        // ===== LOG MODE SETTER =====
        public void SetLogMode(LogMode mode) => _currentMode = mode;

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

                var entries = ReadEntriesFromFile(oldFile, sourceExtension);
                if (entries.Count == 0)
                    continue;

                try
                {
                    _currentStrategy.WriteEntries(newFile, entries);
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

            entry.Username = Environment.UserName;
            entry.MachineName = Environment.MachineName;

            if (_currentMode == LogMode.Centralized || _currentMode == LogMode.Both)
            {
                _ = SendToDocker(entry);
            }

            if (_currentMode == LogMode.Local || _currentMode == LogMode.Both)
            {
                _pendingEntries.Enqueue(entry);
                ScheduleDebouncedWrite();
            }
        }

        private void ScheduleDebouncedWrite()
        {
            Interlocked.Exchange(ref _writePending, 1);
            _debounceTimer?.Change(DebounceMs, Timeout.Infinite);
        }

        // ===== FLUSH =====
        private void FlushDebounced()
        {
            if (Interlocked.CompareExchange(ref _writePending, 0, 1) != 1)
                return;
            FlushCore();
        }

        public void Flush()
        {
            Interlocked.Exchange(ref _writePending, 0);
            _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            FlushCore();
        }

        private void FlushCore()
        {
            var batch = DrainQueue();
            if (batch.Count == 0) return;

            lock (_ioLock)
            {
                try
                {
                    WriteBatch(_currentStrategy, batch);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Local Write Error: {ex.Message}");
                    foreach (var e in batch) _pendingEntries.Enqueue(e);
                }
            }
        }

        private List<ModelLogEntry> DrainQueue()
        {
            var batch = new List<ModelLogEntry>();
            while (_pendingEntries.TryDequeue(out var entry))
                batch.Add(entry);
            return batch;
        }

        private void WriteBatch(ILogFormatStrategy strategy, List<ModelLogEntry> batch)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string extension = strategy.GetFileExtension();
            string filePath = Path.Combine(_logDirectory, $"{today}{extension}");

            if (extension == ".xml")
            {
                WriteXmlBatch(filePath, batch);
            }
            else
            {
                using (var writer = new StreamWriter(filePath, append: true))
                {
                    foreach (var entry in batch)
                        strategy.WriteEntry(writer, entry);
                }
            }
        }

        // ===== DOCKER TRANSMISSION =====
        private async Task SendToDocker(ModelLogEntry entry)
        {
            try
            {
                string machineName = Environment.MachineName;
                string url = $"{DockerUrl}?machine={Uri.EscapeDataString(machineName)}";

                var response = await _httpClient.PostAsJsonAsync(url, entry);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"DOCKER ERROR: {e.Message}");
            }
        }

        // ===== XML HELPER =====
        private void WriteXmlBatch(string filePath, List<ModelLogEntry> batch)
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
                catch { root = new LogEntries(); }
            }
            else { root = new LogEntries(); }

            root.Entries.AddRange(batch);

            var settings = new System.Xml.XmlWriterSettings { Indent = true };
            using (var writer = System.Xml.XmlWriter.Create(filePath, settings))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(LogEntries));
                serializer.Serialize(writer, root);
            }
        }
    }
}
