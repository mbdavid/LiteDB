using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

            _mapper.SetAutoId(document, new LiteCollection<BsonDocument>(_name, _engine, _mapper));

            var doc = _mapper.ToDocument(document);

            _engine.InsertDocuments(_name, new BsonDocument[] { doc });

            return doc["_id"];
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection
        /// </summary>
        public int Insert(IEnumerable<T> docs)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            return _engine.InsertDocuments(_name, this.GetBsonDocs(docs));
        }

        /// <summary>
        /// Convert each T document in a BsonDocument, setting autoId for each one
        /// </summary>
        /// <param name="docs"></param>
        /// <returns></returns>
        private IEnumerable<BsonDocument> GetBsonDocs(IEnumerable<T> docs)
        {
            foreach (var document in docs)
            {
                _mapper.SetAutoId(document, new LiteCollection<BsonDocument>(_name, _engine, _mapper));

                var doc = _mapper.ToDocument(document);

                yield return doc;
            }
        }
    }
}
