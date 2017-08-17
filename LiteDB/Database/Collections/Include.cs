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

            Action<BsonDocument> action = bson => ResolveReferences(bson, path.Split('.'), 0);

            // cloning this collection and adding this include
            var newcol = new LiteCollection<T>(_name, _engine, _mapper, _log);

            newcol._includes.AddRange(_includes);
            newcol._includes.Add(action);

            return newcol;
        }

        /// <summary>
        /// Recursive method for resolving nested DbRef documents.
        /// </summary>
        private void ResolveReferences(BsonDocument bson, string[] pathSegments, int segmentIndex)
        {
            var path = pathSegments[segmentIndex];
            var value = bson[path];

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

                    var bsonValue = doc;

                    if (!colRef.IsString || colId.IsNull)
                    {
                        if (pathSegments.Length == segmentIndex + 1)
                        {
                            // This might be the case when an include is being used *AFTER* another include starting
                            // with the same path, but going deeper.
                            throw LiteException.InvalidDbRef(CreateInvalidPathMessage(pathSegments, segmentIndex));
                        }

                        // Not a reference document, so add the original
                        array.Add(bsonValue);
                    }
                    else
                    {
                        // Resolve this document
                        bsonValue = _engine.Value.Find(colRef, Query.EQ("_id", colId)).FirstOrDefault();

                        // include only object that exists in external ref
                        if (bsonValue != null)
                        {
                            array.Add(bsonValue);
                        }
                    }

                    if (bsonValue != null && segmentIndex < pathSegments.Length - 1)
                    {
                        ResolveReferences(bsonValue.AsDocument, pathSegments, segmentIndex + 1);
                    }
                }
            }
            else if (value.IsDocument)
            {
                // for BsonDocument, get property value update with full object reference
                var doc = value.AsDocument;

                var colRef = doc["$ref"];
                var colId = doc["$id"];

                var bsonValue = doc;

                if (!colRef.IsString || colId.IsNull)
                {
                    if (segmentIndex == pathSegments.Length - 1)
                    {
                        throw LiteException.InvalidDbRef(CreateInvalidPathMessage(pathSegments, segmentIndex));
                    }
                }
                else
                {
                    // Reference document, so replace with real document
                    bsonValue = _engine.Value.Find(colRef, Query.EQ("_id", colId)).FirstOrDefault();
                    bson.Set(path, bsonValue);
                }

                if (segmentIndex < pathSegments.Length - 1)
                {
                    // Not the last property on the path, so resolve nested documents
                    ResolveReferences(bsonValue, pathSegments, segmentIndex + 1);
                }
            }
            else
            {
                throw LiteException.InvalidDbRef(path);
            }
        }

        private static string CreateInvalidPathMessage(string[] names, int depth)
        {
            return string.Format("{0} property ('{1}') could not be resolved in path '{2}'",
                depth, names[depth], string.Join(".", names));
        }
    }
}
