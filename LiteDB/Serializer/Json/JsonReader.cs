using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal class JsonReader
    {
        internal static NumberFormatInfo _enUS = new CultureInfo("en-US").NumberFormat;

        #region Regular expressions

        private static Regex SPACE = new Regex(@"^\s*");
        private static Regex NULL = new Regex(@"^null");
        private static Regex BEGIN_ARRAY = new Regex(@"^\[");
        private static Regex END_ARRAY = new Regex(@"^\]");
        private static Regex EXT_DATA = new Regex(@"^{\s*[""]?\$\w+[""]?\s*:\s*""[^""]*""\s*}");
        private static Regex BEGIN_DOC = new Regex(@"^\{");
        private static Regex END_DOC = new Regex(@"^\}");
        private static Regex BEGIN_STRING = new Regex("^\"");
        private static Regex STRING = new Regex(@"(?:[^""\\]|\\.)*");
        private static Regex NUMBER = new Regex(@"^[-+]?[0-9]*\.?[0-9]+([eE][-+]?[0-9]+)?");
        private static Regex BOOLEAN = new Regex(@"^(true|false)");
        private static Regex KEY = new Regex(@"^[\w$]+");
        private static Regex KEY_VALUE_SEP = new Regex(@"\s*:\s*");
        private static Regex COMMA = new Regex(@"^,");

        #endregion

        public BsonValue Deserialize(string json)
        {
            if(string.IsNullOrEmpty(json)) return new BsonObject();

            var s = new StringScanner(json.Trim());

            return this.ReadValue(s);
        }

        internal BsonValue ReadValue(StringScanner s)
        {
            s.Scan(SPACE);

            if (s.Scan(NULL).Length > 0)
            {
                return BsonValue.Null;
            }
            else if (s.Match(BEGIN_ARRAY))
            {
                return this.ReadArray(s);
            }
            else if (s.Match(EXT_DATA))
            {
                return this.ReadExtendDataType(s);
            }
            else if (s.Match(BEGIN_DOC))
            {
                return this.ReadObject(s);
            }
            else if (s.Match(BEGIN_STRING))
            {
                return new BsonValue(this.ReadString(s));
            }
            else if (s.Match(BOOLEAN))
            {
                return new BsonValue(bool.Parse(s.Scan(BOOLEAN)));
            }
            else if (s.Match(NUMBER))
            {
                return this.ReadNumber(s);
            }

            throw new ArgumentException("String is not a valid JsonEx");
        }

        private BsonObject ReadObject(StringScanner s)
        {
            var obj = new BsonObject();

            s.Scan(BEGIN_DOC);

            while (!s.Match(END_DOC))
            {
                var key = this.ReadKey(s);

                s.Scan(KEY_VALUE_SEP);

                obj[key] = this.ReadValue(s);

                s.Scan(SPACE);

                if(s.Scan(COMMA).Length  == 0) break;
            }

            if (s.Scan(END_DOC).Length == 0) throw new ArgumentException("Missing close json object symbol");

            return obj;
        }

        private BsonArray ReadArray(StringScanner s)
        {
            var arr = new BsonArray();

            s.Scan(BEGIN_ARRAY);

            while(!s.Match(END_ARRAY))
            {
                arr.Add(this.ReadValue(s));

                s.Scan(SPACE);

                if(s.Scan(COMMA).Length  == 0) break;
            }

            if(s.Scan(END_ARRAY).Length == 0) throw new ArgumentException("Missing close json array symbol");

            return arr;
        }

        public IEnumerable<BsonValue> ReadEnumerable(string json)
        {
            var s = new StringScanner(json);

            s.Scan(SPACE);

            if (s.Scan(BEGIN_ARRAY).Length == 0) throw new ArgumentException("String is not a json array");

            while (!s.Match(END_ARRAY))
            {
                yield return this.ReadValue(s);

                s.Scan(SPACE);

                if (s.Scan(COMMA).Length == 0) break;
            }

            if (s.Scan(END_ARRAY).Length == 0) throw new ArgumentException("Missing close json array symbol");

            yield break;
        }

        private string ReadString(StringScanner s)
        {
            if(s.Scan(BEGIN_STRING).Length == 0) throw new ArgumentException("Invalid json string");

            var str = s.ScanUntil(STRING);

            if (s.Scan(BEGIN_STRING).Length == 0) throw new ArgumentException("Invalid json string");

            return str;
        }

        private BsonValue ReadNumber(StringScanner s)
        {
            var nf = CultureInfo.InvariantCulture.NumberFormat;
            var value = s.Scan(NUMBER);

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
            s.Scan(BEGIN_DOC);
            var key = this.ReadKey(s);
            s.Scan(KEY_VALUE_SEP);
            var value = this.ReadString(s);
            s.Scan(SPACE);
            s.Scan(END_DOC);

            try
            {
                switch (key)
                {
                    case "$date": return new BsonValue(DateTime.Parse(value));
                    case "$guid": return new BsonValue(new Guid(value));
                    case "$binary": return new BsonValue(Convert.FromBase64String(value));
                }
            }
            catch (Exception ex)
            {
                throw new FormatException("Invalid " + key + " key in " + value, ex);
            }

            throw new ArgumentException("Invalid json extended format");
        }

        private string ReadKey(StringScanner s)
        {
            s.Scan(SPACE);

            var key = s.Scan(KEY);

            if (key.Length == 0) key = this.ReadString(s);

            return key;
        }
    }
}
