using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Implement an IEnumerable document cache that read data first time and store in memory/disk cache
    /// </summary>
    internal class DocumentEnumerable : IEnumerable<BsonDocument>
    {
        private IEnumerable<BsonDocument> _source;
        private List<PageAddress> _list = new List<PageAddress>();
        private IDocumentLoader _loader;

        public DocumentEnumerable(IEnumerable<BsonDocument> source, IDocumentLoader loader)
        {
            _source = source;
            _loader = loader;
        }

        public IEnumerator<BsonDocument> GetEnumerator()
        {
            return new DocumentEnumerator(_list.Count == 0 ? _source.GetEnumerator() : null, _list, _loader);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new DocumentEnumerator(_list.Count == 0 ? _source.GetEnumerator() : null, _list, _loader);
        }
    }

    internal class DocumentEnumerator : IEnumerator<BsonDocument>
    {
        private IEnumerator<BsonDocument> _source;
        private BsonDocument _current;

        private List<PageAddress> _list;
        private IDocumentLoader _loader;

        private int _index = 0;

        public DocumentEnumerator(IEnumerator<BsonDocument> source, List<PageAddress> list, IDocumentLoader loader)
        {
            _source = source;
            _list = list;
            _loader = loader;
        }

        public BsonDocument Current => _current;
        object IEnumerator.Current => _current;

        public void Dispose()
        {
            _source?.Dispose();
        }

        public bool MoveNext()
        {
            // source != null is first run
            if (_source != null)
            {
                var next = _source.MoveNext();
                _current = _source.Current;
                _list.Add(_current.RawId);
                return next;
            }
            else
            {
                // load document from source
                var rawId = _list[_index++];

                _current = _loader.Load(rawId);

                return _index < _list.Count;
            }
        }

        public void Reset()
        {
            _index = 0;
        }
    }
}