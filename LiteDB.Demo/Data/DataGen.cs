using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class DataGen
    {
        public static IEnumerable<BsonDocument> Movies()
        {
            using (var stream = typeof(DataGen).Assembly.GetManifestResourceStream("LiteDB.Demo.SampleData.movies.json"))
            {
                using(var textReader = new StreamReader(stream))
                {
                    var reader = new JsonReader(textReader);

                    return reader.DeserializeArray().Select(x => x.AsDocument);
                }
            }
        }
    }
}
