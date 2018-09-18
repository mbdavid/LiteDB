using System;
using System.Collections.Generic;
using System.Globalization;
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
            var encoding = GetOption<string>(options, false, "encoding", "utf-8");
            var delimiter = GetOption<string>(options, false, "delimiter", ",");

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
                        writer = new StreamWriter(fs, Encoding.GetEncoding(encoding));
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
                            if (idx++ > 0) writer.Write(delimiter);

                            this.WriteValue(elem.Value, writer);
                        }
                    }
                    else
                    {
                        this.WriteValue(value, writer);
                    }
                }

                if (index > 0)
                {
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

        private void WriteValue(BsonValue value, StreamWriter writer)
        {
            switch(value.Type)
            {
                case BsonType.Null:
                    break;

                case BsonType.Boolean:
                    writer.Write(((Boolean)value.RawValue).ToString().ToLower());
                    break;

                case BsonType.Int32:
                    writer.Write((Int32)value.RawValue);
                    break;

                case BsonType.Int64:
                    writer.Write((Int64)value.RawValue);
                    break;

                case BsonType.Double:
                    writer.Write(((Double)value.RawValue).ToString("0.0########", NumberFormatInfo.InvariantInfo));
                    break;

                case BsonType.Decimal:
                    writer.Write(((Decimal)value.RawValue).ToString("0.0########", NumberFormatInfo.InvariantInfo));
                    break;

                case BsonType.DateTime:
                    writer.Write("\"");
                    writer.Write(((DateTime)value.RawValue).ToUniversalTime().ToString("o"));
                    writer.Write("\"");
                    break;

                case BsonType.Binary:
                    var bytes = (byte[])value.RawValue;
                    writer.Write("\"");
                    writer.Write(Convert.ToBase64String(bytes, 0, bytes.Length));
                    writer.Write("\"");
                    break;

                default:
                    this.WriteString(value.AsString, writer);
                    break;
            }
        }

        /// <summary>
        /// Write string adding quotes (and escaping quote inside string)
        /// </summary>
        private void WriteString(string s, StreamWriter writer)
        {
            writer.Write('\"');

            var l = s.Length;

            for (var index = 0; index < l; index++)
            {
                var c = s[index];

                switch (c)
                {
                    case '\"':
                        writer.Write('"');
                        writer.Write('"');
                        break;

                    default:
                        writer.Write(c);
                        break;
                }
            }

            writer.Write('\"');
        }
    }
}