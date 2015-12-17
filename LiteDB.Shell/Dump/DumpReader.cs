using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    /// <summary>
    /// Dump reader to read data
    /// </summary>
    internal class DumpReader
    {
        private StreamReader _reader;

        public DumpReader(Stream stream)
        {
            _reader = new StreamReader(stream);
        }
    }
}