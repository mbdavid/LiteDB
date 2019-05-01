using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement an IEnumerable document cache that read data first time and store in memory/disk cache
    /// Used in GroupBy operation and MUST read all IEnumerable source before dispose because are need be linear from main resultset
    /// </summary>
    internal class DocumentGroup : IEnumerable<BsonDocument>, IDisposable
    {
        private IEnumerator<BsonDocument> _enumerator;

        private readonly List<PageAddress> _cache = new List<PageAddress>();
        private readonly IDocumentLookup _lookup;
        private readonly BsonValue _key;

        public BsonDocument Root { get; }

        public DocumentGroup(BsonValue key, BsonDocument root, IEnumerable<BsonDocument> source, IDocumentLookup lookup)
        {
            this.Root = root;

            _key = key;
            _enumerator = source.GetEnumerator();
            _lookup = lookup;
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

#if DEBUG
            if (_cache.Count > 0) LOG($"document group cache request (key: {_key}, size: {_cache.Count})", "GROUPBY");
#endif

            // enumerate the _cache first
            for (; index < _cache.Count; index++)
            {
                var rawId = _cache[index];

                yield return _lookup.Load(rawId);
            }

            // continue enumeration of the original _enumerator, until it is finished. 
            // this adds items to the cache and increment 
            for (; _enumerator != null && _enumerator.MoveNext(); index++)
            {
                var current = _enumerator.Current;

                ENSURE(current.RawId.IsEmpty == false, "rawId must have a valid value");

                _cache.Add(current.RawId);

                yield return current;
            }

            if (_enumerator != null)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }

            // other users of the same instance of DocumentEnumerable
            // can add more items to the cache, so we need to enumerate them as well
            for (; index < _cache.Count; index++)
            {
                var rawId = _cache[index];
            
                yield return _lookup.Load(rawId);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}