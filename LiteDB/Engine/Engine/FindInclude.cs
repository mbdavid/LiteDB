using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Find for documents in a collection using Query definition. Support for include reference documents. Use Path syntax
        /// </summary>
        public IEnumerable<BsonDocument> Find(string collection, Query query, string[] includes, int skip = 0, int limit = int.MaxValue)
        {
            if (includes == null) throw new ArgumentNullException(nameof(includes));

            var docs = this.Find(collection, query, skip, limit);

            foreach(var doc in docs)
            {
                // procced with all includes
                foreach(var include in includes)
                {
                    var expr = new BsonExpression(include.StartsWith("$") ? include : "$." + include);

                    // get all values according JSON path
                    foreach(var value in expr.Execute(doc, false)
                        .Where(x => x.IsDocument)
                        .Select(x => x.AsDocument)
                        .ToList())
                    {
                        // works only if is a document
                        var refId = value["$id"];
                        var refCol = value["$ref"];

                        // if has no reference, just go out
                        if (refId.IsNull || !refCol.IsString) continue;

                        // now, find document reference
                        var refDoc = this.FindById(refCol, refId);

                        // if found, change with current document
                        if (refDoc != null)
                        {
                            value.Remove("$id");
                            value.Remove("$ref");

                            refDoc.CopyTo(value);
                        }
                        else
                        {
                            // remove value from parent (document or array)
                            value.Destroy();
                        }
                    }
                }

                yield return doc;
            }
        }
    }
}