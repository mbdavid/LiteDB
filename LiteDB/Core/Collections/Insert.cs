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

            return _engine.InsertDocument(_name, doc);
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection
        /// </summary>
        public int Insert(IEnumerable<T> docs)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            var count = 0;

            foreach (var doc in docs)
            {
                this.Insert(doc);
                count++;
            }

            return count++;
        }
    }
}
