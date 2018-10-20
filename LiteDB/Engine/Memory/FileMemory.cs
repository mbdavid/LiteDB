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
    /// Represent a file in memory
    /// ThreadSafe
    /// </summary>
    public class FileMemory
    {
        private readonly ConcurrentBag<Stream> _pool = new ConcurrentBag<Stream>();

        private IDiskFactory _factory;
        private Stream _stream;

        private Thread _writerThread;
        private Thread _

        public PageBuffer GetPage(long position, bool readOnly)
        {
            return new PageBuffer();
        }

        public 


    }
}