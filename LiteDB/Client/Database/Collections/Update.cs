using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        public bool Update(T document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            // get BsonDocument from object
            var doc = _mapper.ToDocument(document);

            return _engine.Value.Update(_collection, new BsonDocument[] { doc }) > 0;
        }

        /// <summary>
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        public bool Update(BsonValue id, T document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));

            // get BsonDocument from object
            var doc = _mapper.ToDocument(document);

            // set document _id using id parameter
            doc["_id"] = id;

            return _engine.Value.Update(_collection, new BsonDocument[] { doc }) > 0;
        }

        /// <summary>
        /// Update all documents
        /// </summary>
        public int Update(IEnumerable<T> documents)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));

            return _engine.Value.Update(_collection, documents.Select(x => _mapper.ToDocument(x)));
        }

        /// <summary>
        /// Update many document based on merge current document with extend expression (must return a new document). This merge will be applied in all predicate results
        /// Eg: col.UpdateMany("{Name: UPPER(Name)}", "_id > 0")
        /// </summary>
        public int UpdateMany(BsonExpression extend, BsonExpression predicate)
        {
            if (extend == null) throw new ArgumentNullException(nameof(extend));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            if (extend.Type != BsonExpressionType.Document)
            {
                throw new ArgumentException("Extend expression must return a document. Eg: `col.UpdateMany('{ Name: UPPER(Name) }', 'Age > 10')`");
            }

            return _engine.Value.UpdateMany(_collection, extend, predicate);
        }

        /// <summary>
        /// Update many document based on merge current document with extend expression. Use your class with initializers. 
        /// Eg: col.UpdateMany(x => new Customer { Name = x.Name.ToUpper(), Salary: 100 }, x => x.Name == "John")
        /// </summary>
        public int UpdateMany(Expression<Func<T, T>> extend, Expression<Func<T, bool>> predicate)
        {
            if (extend == null) throw new ArgumentNullException(nameof(extend));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            var ext = _mapper.GetExpression(extend);
            var pred = _mapper.GetExpression(predicate);

            if (ext.Type != BsonExpressionType.Document)
            {
                throw new ArgumentException("Extend expression must return an anonymous class to be merge with entities. Eg: `col.UpdateMany(x => new { Name = x.Name.ToUpper() }, x => x.Age > 10)`");
            }

            return _engine.Value.UpdateMany(_collection, ext, pred);
        }
    }
}