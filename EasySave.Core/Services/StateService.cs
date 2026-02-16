using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using EasySave.Core.Models;

namespace EasySave.Core.Services
{
    // ===== STATE SERVICE =====
    public class StateService : IStateService
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

        // ===== PRIVATE MEMBERS =====
        private readonly string _stateFilePath;
        private readonly object _lockObject = new object();
        // N'oublie pas de vérifier que tu as bien : using System.Text.Json.Serialization; en haut du fichier (c'est déjà le cas normalement)

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
    {
        new JsonStringEnumConverter(), // <--- C'est cette ligne qui fait la magie
        new StateDateTimeConverter()
    }
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
        }

        // ===== UPDATE STATE =====

        public void UpdateJobState(BackupJobState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            lock (_lockObject)
            {
                // Read existing states
                List<BackupJobState> allStates = ReadAllStates();

                // Find and update or add the state
                int existingIndex = allStates.FindIndex(s => s.JobName == state.JobName);
                if (existingIndex >= 0)
                {
                    allStates[existingIndex] = state;
                }
                else
                {
                    allStates.Add(state);
                }

                // Write back to file with pretty-print
                WriteAllStates(allStates);
            }
        }

        // ===== PRIVATE HELPERS =====

        private List<BackupJobState> ReadAllStates()
        {
            if (!File.Exists(_stateFilePath))
            {
                return new List<BackupJobState>();
            }

            try
            {
                string json = File.ReadAllText(_stateFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<BackupJobState>();
                }

                var states = JsonSerializer.Deserialize<List<BackupJobState>>(json, _jsonOptions);
                return states ?? new List<BackupJobState>();
            }
            catch (JsonException)
            {
                return new List<BackupJobState>();
            }
        }

        private void WriteAllStates(List<BackupJobState> states)
        {
            string json = JsonSerializer.Serialize(states, _jsonOptions);
            File.WriteAllText(_stateFilePath, json);
        }
    }
}

