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

        public override bool IsFunction => true;

        public override IEnumerable<BsonDocument> Input(BsonValue options)
        {
            if (options == null || (!options.IsString && !options.IsDocument)) throw new LiteException(0, $"Collection ${this.Name} requires a string/object parameter");

            var filename = GetOption<string>(options, true, "filename", null) ?? throw new LiteException(0, $"Collection ${this.Name} requires string as 'filename' or a document field 'filename'");

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
            if (options == null || (!options.IsString && !options.IsDocument)) throw new LiteException(0, "Collection $file_json requires a string/object parameter");

            var filename = GetOption<string>(options, true, "filename", null) ?? throw new LiteException(0, "Collection $file_json requires string as 'filename' or a document field 'filename'");
            var pretty = GetOption<bool>(options, false, "pretty", false);

            var index = 0;

            using (var fs = new FileStream(filename, FileMode.CreateNew))
            {
                using (var writer = new StreamWriter(fs))
                {
                    writer.Write("[");

                    foreach (var value in source)
                    {
                        writer.WriteLine(index++ > 0 ? "," : "");

                        JsonSerializer.Serialize(value, writer, pretty, true);
                    }

                    if (index > 0) writer.WriteLine();

                    writer.WriteLine("]");
                    writer.Flush();
                }
            }

            return index;
        }
    }
}