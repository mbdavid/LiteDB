using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class SysFileCsv : SystemCollection
    {
        public SysFileCsv() : base("$file_csv")
        {
        }

        public override bool IsFunction => true;

        public override IEnumerable<BsonDocument> Input(LiteEngine engine, BsonValue options)
        {
            throw new NotImplementedException();
        }

        public override int Output(IEnumerable<BsonValue> source, BsonValue options)
        {
            if (options == null || (!options.IsString && !options.IsDocument)) throw new LiteException(0, "Collection $file_json requires a string/object parameter");

            var filename = GetOption<string>(options, true, "filename", null) ?? throw new LiteException(0, "Collection $file_json requires string as 'filename' or a document field 'filename'");
            var overwritten = GetOption<bool>(options, false, "overwritten", false);

            var index = 0;

            FileStream fs = null;
            StreamWriter writer = null;
            JsonWriter json = null;

            try
            {
                foreach (var value in source)
                {
                    if (index++ == 0)
                    {
                        fs = new FileStream(filename, overwritten ? FileMode.OpenOrCreate : FileMode.CreateNew);
                        writer = new StreamWriter(fs);
                        json = new JsonWriter(writer)
                        {
                            Encode = false
                        };
                    }
                    else
                    {
                        writer.WriteLine();
                    }

                    if (value.IsDocument)
                    {
                        var doc = value.AsDocument;
                        var idx = 0;

                        foreach (var elem in doc)
                        {
                            if (idx++ > 0) writer.Write(";");

                            json.Serialize(elem.Value);
                        }
                    }
                    else
                    {
                        json.Serialize(value);
                    }
                }

                if (index > 0)
                {
                    writer.WriteLine();
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