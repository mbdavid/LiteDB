using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class SysFileJson : SystemCollection
    {
        public SysFileJson() : base("$file_json")
        {
        }

        public override IEnumerable<BsonDocument> Input(BsonValue options)
        {
            if (options == null) throw new LiteException(0, "Collection $file_json requires a string/object parameter");

            var filename = options.AsString;

            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fs))
                {
                    var json = new JsonReader(reader);

                    var source = json.DeserializeArray()
                        .Select(x => x.AsDocument);

                    // read documents inside file and return one-by-one
                    foreach (var doc in source)
                    {
                        yield return doc;
                    }
                }
            }
        }

        public override int Output(IEnumerable<BsonValue> source, BsonValue options)
        {
            if (options == null) throw new LiteException(0, "Collection $file_json requires a string/object parameter");

            var filename = options.AsString;
            var index = 0;
            var pretty = false;

            using (var fs = new FileStream(filename, FileMode.CreateNew))
            {
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine("[");

                    foreach (var value in source)
                    {
                        if (index++ > 0) writer.Write(",");

                        var json = JsonSerializer.Serialize(value, pretty, true);

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