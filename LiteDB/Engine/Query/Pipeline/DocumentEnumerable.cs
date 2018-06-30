using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement an IEnumerable document cache that read data first time and store in memory/disk cache
    /// Used in GroupBy operation and MUST read all IEnumerable source before dispose because are need be linear from main resultset
    /// </summary>
    internal class DocumentEnumerable : IEnumerable<BsonDocument>, IDisposable
    {
        private IEnumerator<BsonDocument> _enumerator;
        private List<PageAddress> _cache = new List<PageAddress>();
        private IDocumentLoader _loader;

        public DocumentEnumerable(IEnumerable<BsonDocument> source, IDocumentLoader loader)
        {
            _enumerator = source.GetEnumerator();
            _loader = loader;
        }

        public void Dispose()
        {
            // must read all enumerable before dispose
            if (_enumerator != null)
            {
                while (_enumerator.MoveNext()) ;
                _enumerator.Dispose();
                _enumerator = null;
            }
        }

        public IEnumerator<BsonDocument> GetEnumerator()
        {
            // https://stackoverflow.com/a/34633464/3286260

            // the index of the current item in the cache.
            var index = 0;

            // enumerate the _cache first
            for (; index < _cache.Count; index++)
            {
                var rawId = _cache[index];

                yield return _loader.Load(rawId);
            }

            // continue enumeration of the original _enumerator, until it is finished. 
            // this adds items to the cache and increment 
            for (; _enumerator != null && _enumerator.MoveNext(); index++)
            {
                var current = _enumerator.Current;
                _cache.Add(current.RawId);
                yield return current;
            }

            if (_enumerator != null)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }

            // some other users of the same instance of DocumentEnumerable
            // can add more items to the cache, so we need to enumerate them as well
            for (; index < _cache.Count; index++)
            {
                var rawId = _cache[index];
            
                yield return _loader.Load(rawId);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}