using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    public class StateService : IStateService
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        private const int DebounceMs = 300;

        // ===== PRIVATE MEMBERS =====
        private readonly string _stateFilePath;
        private readonly ConcurrentDictionary<string, BackupJobState> _states = new();
        private readonly object _writeLock = new object();
        private readonly Timer? _debounceTimer;
        private volatile int _writePending;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(), new StateDateTimeConverter() }
        };

        private sealed class StateDateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var s = reader.GetString();
                if (string.IsNullOrEmpty(s))
                    return default;
                return DateTime.TryParseExact(s, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    ? dt
                    : DateTime.Parse(s);
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString(TimestampFormat, CultureInfo.InvariantCulture));
            }
        }

        // ===== CONSTRUCTOR =====

        public StateService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string stateDirectory = Path.Combine(appDataPath, "EasySave");

            if (!Directory.Exists(stateDirectory))
            {
                Directory.CreateDirectory(stateDirectory);
            }

            _stateFilePath = Path.Combine(stateDirectory, "state.json");
            LoadFromDisk();

            _debounceTimer = new Timer(_ => FlushDebounced(), null, Timeout.Infinite, Timeout.Infinite);
        }

        // ===== IStateService =====

        public void UpdateJobState(BackupJobState state)
        {
            ArgumentNullException.ThrowIfNull(state);

            BackupJobState snapshot = CloneState(state);
            _states[state.JobName] = snapshot;
            ScheduleDebouncedWrite();
        }

        public void Flush()
        {
            Interlocked.Exchange(ref _writePending, 0);
            _debounceTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            WriteToDisk();
        }

        // ===== DEBOUNCED WRITE =====

        private void ScheduleDebouncedWrite()
        {
            Interlocked.Exchange(ref _writePending, 1);
            _debounceTimer?.Change(DebounceMs, Timeout.Infinite);
        }

        private void FlushDebounced()
        {
            if (Interlocked.CompareExchange(ref _writePending, 0, 1) != 1)
                return;
            WriteToDisk();
        }

        private void WriteToDisk()
        {
            lock (_writeLock)
            {
                var snapshot = _states.Values.ToList();
                if (snapshot.Count == 0)
                    return;

                string json = JsonSerializer.Serialize(snapshot, JsonOptions);
                File.WriteAllText(_stateFilePath, json);
            }
        }

        // ===== DISK LOAD =====

        private void LoadFromDisk()
        {
            if (!File.Exists(_stateFilePath))
                return;

            try
            {
                string json = File.ReadAllText(_stateFilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var states = JsonSerializer.Deserialize<List<BackupJobState>>(json, JsonOptions);
                if (states == null)
                    return;

                foreach (var s in states)
                {
                    if (!string.IsNullOrEmpty(s?.JobName))
                        _states[s.JobName] = s;
                }
            }
            catch (JsonException)
            {
                // Corrupt or incompatible file; start fresh
            }
        }

        // ===== HELPERS =====

        private static BackupJobState CloneState(BackupJobState source)
        {
            var json = JsonSerializer.Serialize(source, JsonOptions);
            return JsonSerializer.Deserialize<BackupJobState>(json, JsonOptions) ?? new BackupJobState(source.JobName);
        }
    }
}

