using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class JsonReader
    {
        internal static NumberFormatInfo _enUS = new CultureInfo("en-US").NumberFormat;

        public BsonValue Deserialize(string json)
        {
            if(string.IsNullOrEmpty(json)) return new BsonObject();

            var s = new StringScanner(json.Trim());

            return this.ReadValue(s);
        }

        internal BsonValue ReadValue(StringScanner s)
        {
            s.Scan(@"\s*");

            if (s.Scan("null").Length > 0)
            {
                return BsonValue.Null;
            }
            else if (s.Match(@"\["))
            {
                return this.ReadArray(s);
            }
            else if (s.Match(@"{\s*[""]?\$\w+[""]?\s*:\s*""[^""]*""\s*}"))
            {
                return this.ReadExtendDataType(s);
            }
            else if (s.Match(@"{"))
            {
                return this.ReadObject(s);
            }
            else if (s.Match("\""))
            {
                return new BsonValue(this.ReadString(s));
            }
            else if (s.Match(@"[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?"))
            {
                return this.ReadNumber(s);
            }
            else if (s.Match(@"(true|false)"))
            {
                return new BsonValue(bool.Parse(s.Scan("(true|false)")));
            }

            throw new ArgumentException("String is not a valid JsonEx");
        }

        private BsonObject ReadObject(StringScanner s)
        {
            var obj = new BsonObject();

            s.Scan(@"\s*{");

            while (!s.Match(@"\s*}"))
            {
                s.Scan(@"\s*");

                // accept key without "
                var key = s.Match(@"[\w$]+") ? 
                    s.Scan(@"[\w$]+") : this.ReadString(s);

                if (key.Trim().Length == 0) throw new ArgumentException("Invalid json object key");

                s.Scan(@"\s*:\s*");

                var value = this.ReadValue(s);

                obj[key] = value;

                if(s.Scan(@"\s*,").Length  == 0) break;
            }

            if (s.Scan(@"\s*}").Length == 0) throw new ArgumentException("Missing close json object symbol");

            return obj;
        }

        private BsonArray ReadArray(StringScanner s)
        {
            var arr = new BsonArray();

            s.Scan(@"\s*\[");

            while(!s.Match(@"\s*\]"))
            {
                var value = this.ReadValue(s);

                arr.Add(value);

                if(s.Scan(@"\s*,").Length  == 0) break;
            }

            if(s.Scan(@"\s*\]").Length == 0) throw new ArgumentException("Missing close json array symbol");

            return arr;
        }

        public IEnumerable<BsonValue> ReadEnumerable(string json)
        {
            var s = new StringScanner(json);

            if (s.Scan(@"\s*\[").Length == 0) throw new ArgumentException("String is not a json array");

            while (!s.Match(@"\s*\]"))
            {
                yield return this.ReadValue(s);

                if (s.Scan(@"\s*,").Length == 0) break;
            }

            if (s.Scan(@"\s*\]").Length == 0) throw new ArgumentException("Missing close json array symbol");

            yield break;
        }

        private string ReadString(StringScanner s)
        {
            if(s.Scan(@"\s*\""").Length == 0) throw new ArgumentException("Invalid json string");

            var str = s.ScanUntil(@"(?:[^""\\]|\\.)*");

            if (s.Scan("\"").Length == 0) throw new ArgumentException("Invalid json string");

            return str;
        }

        private BsonValue ReadNumber(StringScanner s)
        {
            var nf = CultureInfo.InvariantCulture.NumberFormat;
            var value = s.Scan(@"[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?");

            if (value.Contains("."))
            {
                return new BsonValue(Convert.ToDouble(value, nf));
            }
            else
            {
                return new BsonValue(Convert.ToInt32(value));
            }
        }

        private BsonValue ReadExtendDataType(StringScanner s)
        {
            s.Scan(@"\{\s*");
            var dataType = s.Match(@"[$\w]+") ? s.Scan(@"[$\w]+") : this.ReadString(s);
            s.Scan(@"\s*:\s*");
            var value = this.ReadString(s);
            s.Scan(@"\s*}");

            try
            {
                switch (dataType)
                {
                    case "$date": return new BsonValue(DateTime.Parse(value));
                    case "$guid": return new BsonValue(new Guid(value));
                    case "$binary": return new BsonValue(Convert.FromBase64String(value));
                }
            }
            catch (Exception ex)
            {
                throw new FormatException("Invalid " + dataType + " value in " + value, ex);
            }

            throw new ArgumentException("Invalid JSON extended format");
        }
    }
}
