using EasyLog.Models;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasyLog.Services.Strategies
{
    public class JsonLogStrategy : ILogFormatStrategy
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public void WriteEntry(StreamWriter writer, ModelLogEntry entry)
        {
            string json = JsonSerializer.Serialize(entry, _options);
            writer.WriteLine(json);
            writer.WriteLine();
        }

        public void WriteEntries(string filePath, List<ModelLogEntry> entries)
        {
            using (var writer = new StreamWriter(filePath, append: false))
            {
                foreach (var entry in entries)
                    WriteEntry(writer, entry);
            }
        }

        public string GetFileExtension() => ".json";
    }
}
     