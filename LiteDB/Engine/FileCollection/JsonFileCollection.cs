using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent an external file with an array data source
    /// </summary>
    public class JsonFileCollection : IFileCollection
    {
        private readonly string _filename;
        private readonly bool _pretty;

        public JsonFileCollection(string filename, bool pretty = true)
        {
            _filename = filename;
            _pretty = pretty;
        }

        public string Name => Path.GetFileName(_filename);

        public IEnumerable<BsonDocument> Input()
        {
            using (var fs = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fs))
                {
                    var json = new JsonReader(reader);

                    var source = json.DeserializeArray()
                        .Select(x => x.AsDocument);

                    // read documents inside file and return one-by-one
                    foreach(var doc in source)
                    {
                        yield return doc;
                    }
                }
            }
        }

        public int Output(IEnumerable<BsonValue> source)
        {
            var index = 0;

            using (var fs = new FileStream(_filename, FileMode.CreateNew))
            {
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine("[");

                    foreach (var value in source)
                    {
                        if (index++ > 0) writer.Write(",");

                        var json = JsonSerializer.Serialize(value, _pretty, true);

                        writer.WriteLine(json);
                    }

                    writer.Write("]");
                    writer.Flush();
                }
            }

            return index;
        }
    }
}
