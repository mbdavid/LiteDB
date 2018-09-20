using LiteDB.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public static class LiteEngineExtensions
    {
        public static T[] ExecuteValues<T>(this LiteEngine engine, string sql, BsonDocument args = null)
        {
            var values = new List<T>();
            var mapper = new BsonMapper();

            using (var r = engine.Execute(sql, args ?? new BsonDocument()))
            {
                while(r.Read())
                {
                    var doc = r.Current.AsDocument;
                    var key = doc.Keys.First();

                    values.Add(mapper.Deserialize<T>(doc[key]));
                }
            }

            return values.ToArray();
        }
    }
}