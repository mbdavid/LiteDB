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
        private readonly static IFormatProvider _numberFormat = CultureInfo.InvariantCulture.NumberFormat;

        public SysFileCsv() : base("$file_csv")
        {
        }

        public override bool IsFunction => true;

        public override IEnumerable<BsonDocument> Input(LiteEngine engine, BsonValue options)
        {
            throw new NotImplementedException();
        }

        public override int Output(IEnumerable<BsonDocument> source, BsonValue options)
        {
            if (options == null || (!options.IsString && !options.IsDocument)) throw new LiteException(0, "Collection $file_json requires a string/object parameter");

            var filename = GetOption<string>(options, true, "filename", null) ?? throw new LiteException(0, "Collection $file_json requires string as 'filename' or a document field 'filename'");
            var overwritten = GetOption<bool>(options, false, "overwritten", false);
            var encoding = GetOption<string>(options, false, "encoding", "utf-8");
            var delimiter = GetOption<string>(options, false, "delimiter", ",");
            var header = GetOption<bool>(options, false, "header", true);

            var index = 0;

            FileStream fs = null;
            StreamWriter writer = null;

            try
            {
                foreach (var doc in source)
                {
                    if (index++ == 0)
                    {
                        fs = new FileStream(filename, overwritten ? FileMode.OpenOrCreate : FileMode.CreateNew);
                        writer = new StreamWriter(fs, Encoding.GetEncoding(encoding));

                        // print file header
                        if (header)
                        {
                            var idxHeader = 0;

                            foreach (var elem in doc)
                            {
                                if (idxHeader++ > 0) writer.Write(delimiter);
                                writer.Write(elem.Key);
                            }

                            writer.WriteLine();
                        }
                    }
                    else
                    {
                        writer.WriteLine();
                    }

                    var idxValue = 0;

                    foreach (var elem in doc)
                    {
                        if (idxValue++ > 0) writer.Write(delimiter);

                        this.WriteValue(elem.Value, writer);
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
                    writer.Write(((Int32)value.RawValue).ToString(_numberFormat));
                    break;

                case BsonType.Int64:
                    writer.Write(((Int64)value.RawValue).ToString(_numberFormat));
                    break;

                case BsonType.Double:
                    writer.Write(((Double)value.RawValue).ToString(_numberFormat));
                    break;

                case BsonType.Decimal:
                    writer.Write(((Decimal)value.RawValue).ToString(_numberFormat));
                    break;

                case BsonType.DateTime:
                    writer.Write(((DateTime)value.RawValue).ToUniversalTime().ToString("o"));
                    break;

                case BsonType.Binary:
                    var bytes = (byte[])value.RawValue;
                    writer.Write(Convert.ToBase64String(bytes, 0, bytes.Length));
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