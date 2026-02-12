using EasyLog.Models;
using System.Xml.Serialization;

namespace EasyLog.Services.Strategies
{
    public class XmlLogStrategy : ILogFormatStrategy
    {
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(ModelLogEntry));

        public void WriteEntry(StreamWriter writer, ModelLogEntry entry)
        {
            _serializer.Serialize(writer, entry);
            writer.WriteLine(); //blank line
        }

        public string GetFileExtension() => ".xml";
    }
}
