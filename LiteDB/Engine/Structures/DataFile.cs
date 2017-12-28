using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// Represents a pair of Datafile and WalFile in Stream
    /// </summary>
    internal class DataFile
    {
        public Stream Data { get; set; }
        public string Wal { get; set; }
    }
}