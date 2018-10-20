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
    /// ThreadSafe (single instance can be shared across threads for read-only)
    /// </summary>
    public struct PageBuffer : IDisposable
    {
        public long Posistion;
        public int ReadCounter;
        public bool IsDirty;

        ArraySegment<byte> Buffer;

        public void Dispose()
        {
            Interlocked.Decrement(ref ReadCounter);
        }
    }
}