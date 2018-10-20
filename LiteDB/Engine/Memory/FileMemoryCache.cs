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
    /// ThreadSafe
    /// </summary>
    internal class FileMemoryCache
    {
        private readonly ConcurrentDictionary<long, PageBuffer> _cache = new ConcurrentDictionary<long, PageBuffer>();

        private readonly Thread _thread;
        private readonly ManualResetEventSlim _waiter;
        private bool _running = true;

        public FileMemoryCache()
        {
            _waiter = new ManualResetEventSlim(false);
            _thread = new Thread(this.CreateThread);
            _thread.Name = "LiteDB_CacheCleaner";
            _thread.Start();
        }

        public PageBuffer GetOrAddPage(long position, Func<long, PageBuffer> addFactory)
        {
            return _cache.GetOrAdd(position, addFactory);
        }

        public bool TryGetPage(long position, out PageBuffer page)
        {
            return _cache.TryGetValue(position, out page);
        }

        public void AddPage(PageBuffer page)
        {
            var result = _cache.TryAdd(page.Posistion, page);

            DEBUG(result == false, "add page always must be possible add (this page is new)");
        }


        private void CreateThread()
        {
            // tem que fazer lock da _cache
            // remove todas que o share == 0
            // devolve pra memorystore


        }

    }
}