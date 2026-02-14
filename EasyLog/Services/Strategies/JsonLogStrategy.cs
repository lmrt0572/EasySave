using EasyLog.Models;
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
            writer.WriteLine(); //blank line
        }

        public string GetFileExtension() => ".json";
    }
}
     