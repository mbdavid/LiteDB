using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Implement a Stream queue to get/release 
    /// </summary>
    internal class DiskService : IDisposable
    {
        private ConcurrentBag<Stream> _dataPool = new ConcurrentBag<Stream>();
        private ConcurrentBag<Stream> _walPool = new ConcurrentBag<Stream>();
        private IDiskFactory _factory;
        private TimeSpan _timeout;

        public DiskService(IDiskFactory factory, TimeSpan timeout)
        {
            _factory = factory;
            _timeout = timeout;
        }

        /// <summary>
        /// Get a new or re-used datafile stream from pool
        /// </summary>
        public DiskFile GetDisk(bool includeWal)
        {
            var file = new DiskFile((d, w) =>
            {
                _dataPool.Add(d);
                if (w != null) _walPool.Add(w);
            });

            // get (removing) datafile stream from pool or create a new if pool are empty
            if (_dataPool.TryTake(out var data))
            {
                file.Data = data;
            }
            else
            {
                file.Data = _factory.GetDataFile();
            }

            if (includeWal)
            {
                // get (removing) walfile stream from pool or create a new if pool are empty
                if (_walPool.TryTake(out var wal))
                {
                    file.Wal = wal;
                }
                else
                {
                    file.Wal = _factory.GetWalFile();
                }
            }

            return file;
        }

        /// <summary>
        /// Dispose all stream in pool
        /// </summary>
        public void Dispose()
        {
            if (!_factory.Dispose) return;

            while (_dataPool.TryTake(out var data))
            {
                data.Dispose();
            }
            while (_walPool.TryTake(out var wal))
            {
                wal.Dispose();
            }
        }
    }
}