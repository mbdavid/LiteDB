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
    public class SampleData
    {
        public IEnumerable<BsonDocument> GetMovies()
        {
            using (var stream = typeof(SampleData).Assembly.GetManifestResourceStream("LiteDB.Demo.SampleData.movies.json"))
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
