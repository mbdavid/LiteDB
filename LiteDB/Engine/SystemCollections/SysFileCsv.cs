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
            var filename = GetOption(options, "filename", null).AsString ?? throw new LiteException(0, $"Collection ${this.Name} requires string as 'filename' or a document field 'filename'");
            var encoding = GetOption(options, "encoding", "utf-8").AsString;
            var delimiter = GetOption(options, "delimiter", ",").AsString[0];

            // read header (or first line as header)
            var header = new List<string>();

            if (options.IsDocument && options["header"].IsArray)
            {
                header.AddRange(options["header"].AsArray.Select(x => x.AsString));
            }

            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fs, Encoding.GetEncoding(encoding)))
                {
                    // if not header declared, use first line as header fields
                    if (header.Count == 0)
                    {
                        while (true)
                        {
                            var key = this.ReadString(reader, delimiter, out var newLine);

                            if (key == null) break;

                            header.Add(key);

                            if (newLine) break;
                        }
                    }

                    // read all results
                    var index = 0;
                    var doc = new BsonDocument();

                    while(true)
                    {
                        var value = this.ReadString(reader, delimiter, out var newLine);

                        if (value == null) yield break;

                        if (index < header.Count)
                        {
                            var key = header[index++];
                            doc[key] = value;
                        }

                        if (newLine)
                        {
                            yield return doc;

                            doc = new BsonDocument();
                            index = 0;
                        }
                    }

                }
            }
        }

        public override int Output(IEnumerable<BsonDocument> source, BsonValue options)
        {
            var filename = GetOption(options, "filename", null).AsString ?? throw new LiteException(0, "Collection $file_json requires string as 'filename' or a document field 'filename'");
            var overwritten = GetOption(options, "overwritten", false).AsBoolean;
            var encoding = GetOption(options, "encoding", "utf-8").AsString;
            var delimiter = GetOption(options, "delimiter", ",").AsString[0];
            var header = GetOption(options, "header", true).AsBoolean;

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

                        if (elem.Value.IsNull == false)
                        {
                            this.WriteString(elem.Value.AsString, writer);
                        }
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

        private string ReadString(TextReader reader, char delimiter, out bool newLine)
        {
            var sb = new StringBuilder();
            var c = reader.Read();

            // eat possible new line before read string
            while(c == '\n' || c == '\r')
            {
                c = reader.Read();
            }

            if (c == -1)
            {
                newLine = true;
                return null;
            }

            // read " string
            if(c == '"')
            {
                var last = c;

                while(c != -1)
                {
                    c = reader.Read();

                    if (c == '"')
                    {
                        var next = reader.Read();

                        if (next == '"')
                        {
                            sb.Append('"');
                            continue;
                        }
                        else
                        {
                            c = next;
                            break;
                        }
                    }

                    sb.Append((char)c);
                    last = c;
                }
            }
            else
            {
                while(!(c == '\n' || c == '\r' || c == delimiter || c == -1))
                {
                    sb.Append((char)c);
                    c = reader.Read();
                }
            }

            newLine = (c == '\n' || c == '\r');

            return sb.ToString();
        }
    }
}