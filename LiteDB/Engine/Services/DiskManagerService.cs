using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    internal class DiskManagerService : IDisposable
    {
        private ConcurrentBag<Stream> _pool = new ConcurrentBag<Stream>();
        private Func<Stream> _factory;
        private int _limit;

        public DiskManagerService(Func<Stream> factory, int limit)
        {
            _factory = factory;
            _limit = limit;
        }

        /// <summary>
        /// Get how many open stream are in stream pool
        /// </summary>
        private int Count => _pool.Count;

        /// <summary>
        /// Get a re-used stream or create new if no more inside pool
        /// </summary>
        public Stream GetStream()
        {
            // if pool contains stream, remove and return
            if (_pool.TryTake(out var stream))
            {
                return stream;
            }

            // otherwise create new stream (when release will be added to pool)
            return _factory();
        }

        public void Release(Stream stream)
        {
            // release stream to pool only if pool are over limit, otherside, dispose
            if (_pool.Count < _limit)
            {
                _pool.Add(stream);
            }
            else
            {
                stream.Dispose();
            }
        }

        /// <summary>
        /// Dispose all stream in pool
        /// </summary>
        public void Dispose()
        {
            while(_pool.TryTake(out var stream))
            {
                stream.Dispose();
            }
        }
    }
}