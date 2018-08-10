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
            var overwritten = GetOption<bool>(options, false, "overwritten", false);

            var index = 0;
            FileStream fs = null;
            StreamWriter writer = null;

            try
            {
                foreach (var value in source)
                {
                    if (index++ == 0)
                    {
                        fs = new FileStream(filename, overwritten ? FileMode.OpenOrCreate : FileMode.CreateNew);
                        writer = new StreamWriter(fs);
                        writer.WriteLine("[");
                    }
                    else
                    {
                        writer.WriteLine(",");
                    }

                    JsonSerializer.Serialize(value, writer, pretty, true);
                }

                if (index > 0)
                {
                    writer.WriteLine();
                    writer.Write("]");
                    writer.Flush();
                }
            }
            finally
            {
                if (writer != null) writer.Dispose();
                if (fs != null) fs.Dispose();
            }

            return index;
        }
    }
}