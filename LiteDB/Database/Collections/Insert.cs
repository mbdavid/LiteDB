using System;
using System.Collections.Generic;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Insert a new document to this collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        public BsonValue Insert(T document)
        {
            if (document == null) throw new ArgumentNullException("document");

            _mapper.SetAutoId(document, _engine.Value, _name);

            var doc = _mapper.ToDocument(document);

            return _engine.Value.Insert(_name, doc);
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection. Can be set buffer size to commit at each N documents
        /// </summary>
        public int Insert(IEnumerable<T> docs)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            return _engine.Value.Insert(_name, this.GetBsonDocs(docs));
        }

        /// <summary>
        /// Convert each T document in a BsonDocument, setting autoId for each one
        /// </summary>
        private IEnumerable<BsonDocument> GetBsonDocs(IEnumerable<T> docs)
        {
            foreach (var doc in docs)
            {
                _mapper.SetAutoId(doc, _engine.Value, _name);

                yield return _mapper.ToDocument(doc);
            }
        }
    }
}