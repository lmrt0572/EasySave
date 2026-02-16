using EasyLog.Models;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace EasyLog.Services.Strategies
{
    public class XmlLogStrategy : ILogFormatStrategy
    {
        private static readonly XmlSerializer _entrySerializer = new XmlSerializer(typeof(ModelLogEntry));
        private static readonly XmlSerializer _rootSerializer = new XmlSerializer(typeof(LogEntries));

        public void WriteEntry(StreamWriter writer, ModelLogEntry entry)
        {
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true
            };

            using (var xmlWriter = XmlWriter.Create(writer, settings))
            {
                _entrySerializer.Serialize(xmlWriter, entry);
            }

            writer.WriteLine();
            writer.WriteLine();
        }

        public void WriteEntries(string filePath, List<ModelLogEntry> entries)
        {
            var root = new LogEntries { Entries = entries };
            var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = false };
            using (var writer = XmlWriter.Create(filePath, settings))
            {
                _rootSerializer.Serialize(writer, root);
            }
        }

        public string GetFileExtension() => ".xml";
    }
}
