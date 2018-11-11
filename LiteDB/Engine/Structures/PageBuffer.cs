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
    internal class PageBuffer : ArraySlice<byte>
    {
        /// <summary>
        /// Get/Set page position. If page are writable, this postion CAN be MaxValue (has not defined position yet)
        /// </summary>
        public long Position;

        /// <summary>
        /// Get/Set how many read-share threads are using this page. -1 means 1 thread are using as writable
        /// </summary>
        public int ShareCounter;

        /// <summary>
        /// Get/Set timestamp from last request
        /// </summary>
        public long Timestamp;

        public PageBuffer(byte[] buffer, int offset)
            : base(buffer, offset, PAGE_SIZE)
        {
            this.Position = long.MaxValue;
            this.Timestamp = 0;
            this.ShareCounter = 0;
        }
    }
}