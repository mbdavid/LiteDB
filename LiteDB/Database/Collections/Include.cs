using System;
using System.Linq;
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

            var path = _visitor.GetField(dbref);

            return this.Include(path);
        }

        /// <summary>
        /// Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents
        /// Returns a new Collection with this action included
        /// </summary>
        public LiteCollection<T> Include(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            Action<BsonDocument> action = (bson) =>
            {
                var value = bson.Get(path);

                if (value.IsNull) return;

                // if property value is an array, populate all values
                if (value.IsArray)
                {
                    var array = value.AsArray;
                    if (array.Count == 0) return;

                    var docs = array.ToArray();
                    array.Clear();

                    foreach (var doc in docs)
                    {
                        var colRef = doc.AsDocument["$ref"];
                        var colId = doc.AsDocument["$id"];

                        if (!colRef.IsString || colId.IsNull) throw LiteException.InvalidDbRef(path);

                        var obj = _engine.Value.Find(colRef, Query.EQ("_id", colId)).FirstOrDefault();

                        // include only object that exists in external ref
                        if(obj != null)
                        {
                            array.Add(obj);
                        }
                    }
                }
                else if(value.IsDocument)
                {
                    // for BsonDocument, get property value update with full object reference
                    var doc = value.AsDocument;

                    var colRef = doc["$ref"];
                    var colId = doc["$id"];

                    if (!colRef.IsString || colId.IsNull) throw LiteException.InvalidDbRef(path);

                    var obj = _engine.Value.Find(colRef, Query.EQ("_id", colId)).FirstOrDefault();

                    bson.Set(path, obj);
                }
                else
                {
                    throw LiteException.InvalidDbRef(path);
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