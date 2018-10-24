using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent page buffer to be read/write using FileMemory
    /// </summary>
    internal class PageBuffer
    {
        public long Position;
        public int ShareCounter;
        public ArraySegment<byte> Buffer;
    }
}