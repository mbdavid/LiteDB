using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

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

            var propPath = dbref.GetPath();

            Action<BsonDocument> action = (bson) =>
            {
                var prop = bson.Get(propPath);

                if(prop.IsNull) return;

                // if property value is an array, populate all values
                if(prop.IsArray)
                {
                    var array = prop.AsArray;
                    if(array.Count == 0) return;

                    // all doc refs in an array must be same collection, lets take first only
                    var col = this.Database.GetCollection(array[0].AsDocument["$ref"]);

                    for(var i = 0; i < array.Count; i++)
                    {
                        var obj = col.FindById(array[i].AsDocument["$id"]);
                        array[i] = obj;
                    }
                }
                else
                {
                    // for BsonDocument, get property value e update with full object refence
                    var doc = prop.AsDocument;
                    var col = this.Database.GetCollection(doc["$ref"]);
                    var obj = col.FindById(doc["$id"]);
                    bson.Set(propPath, obj);
                }
            };

            // cloning this collection and adding this include
            var newcol = new LiteCollection<T>(this.Database, this.Name);

            newcol._pageID = _pageID;
            newcol._includes.AddRange(_includes);
            newcol._includes.Add(action);

            return newcol;
        }
    }
}
