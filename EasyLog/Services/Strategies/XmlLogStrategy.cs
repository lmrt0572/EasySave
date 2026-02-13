using EasyLog.Models;
using System.Xml;
using System.Xml.Serialization;

namespace EasyLog.Services.Strategies
{
    public class XmlLogStrategy : ILogFormatStrategy
    {
        private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(ModelLogEntry));

        public void WriteEntry(StreamWriter writer, ModelLogEntry entry)
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            using (var xmlWriter = XmlWriter.Create(writer, settings))
            {
                _serializer.Serialize(xmlWriter, entry);
            }

            writer.WriteLine(); 
            writer.WriteLine(); // blank lines
        }

        public string GetFileExtension() => ".xml";
    }
}
