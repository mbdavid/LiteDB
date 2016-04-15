using System;
using System.Linq.Expressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include<K>(Expression<Func<T, K>> dbref)
        {
            if (dbref == null) throw new ArgumentNullException("dbref");

            var path = _visitor.GetBsonField(dbref);

            Action<BsonDocument> action = (bson) =>
            {
                var value = bson.Get(path);

                if (value.IsNull) return;

                // if property value is an array, populate all values
                if (value.IsArray)
                {
                    var array = value.AsArray;
                    if (array.Count == 0) return;

                    // all doc refs in an array must be same collection, lets take first only
                    var col = new LiteCollection<BsonDocument>(array[0].AsDocument["$ref"], _engine, _mapper, _log);

                    for (var i = 0; i < array.Count; i++)
                    {
                        var obj = col.FindById(array[i].AsDocument["$id"]);
                        array[i] = obj;
                    }
                }
                else
                {
                    // for BsonDocument, get property value e update with full object refence
                    var doc = value.AsDocument;
                    var col = new LiteCollection<BsonDocument>(doc["$ref"], _engine, _mapper, _log);
                    var obj = col.FindById(doc["$id"]);
                    bson.Set(path, obj);
                }
            };

            // cloning this collection and adding this include
            var newcol = new LiteCollection<T>(_name, _engine, _mapper, _log);

            newcol._includes.AddRange(_includes);
            newcol._includes.Add(action);

            return newcol;
        }
    }
}