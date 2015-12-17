using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    /// <summary>
    /// Dump writer to export LiteDB data to flat file/memory
    /// File format (lines that starts with):
    /// # comments
    /// = init new collection
    /// + define collection index
    /// { document
    /// </summary>
    internal class DumpWriter : IDisposable
    {
        private StreamWriter _writer;

        public DumpWriter(Stream stream)
        {
            _writer = new StreamWriter(stream);

            _writer.WriteLine("# LiteDB Export = " + DateTime.Now);
        }

        public void WriteCollection(string colName)
        {
            _writer.WriteLine("= " + colName);
        }

        public void WriteIndex(string field, string options)
        {
            _writer.WriteLine("+ " + field + " : " + options);
        }

        public void WriteDocument(string json)
        {
            _writer.WriteLine(json.Replace(@"\n", "\\n"));
        }

        public void Dispose()
        {
            _writer.Flush();
        }
    }
}