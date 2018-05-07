using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// A class that read a json string using a tokenizer (without regex)
    /// </summary>
    internal class JsonReader
    {
        private JsonTokenizer _tokenizer = null;

        public long Position { get { return _tokenizer.Position; } }

        public JsonReader(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            _tokenizer = new JsonTokenizer(reader);
        }

        public BsonValue Deserialize()
        {
            var token = _tokenizer.ReadToken();

            if (token.TokenType == JsonTokenType.EOF) return BsonValue.Null;

            var value = this.ReadValue(token);

            return value;
        }

        public IEnumerable<BsonValue> DeserializeArray()
        {
            var token = _tokenizer.ReadToken();

            if (token.TokenType == JsonTokenType.EOF) yield break;

            token.Expect(JsonTokenType.BeginArray);

            token = _tokenizer.ReadToken();

            while (token.TokenType != JsonTokenType.EndArray)
            {
                yield return this.ReadValue(token);

                token = _tokenizer.ReadToken();

                if (token.TokenType == JsonTokenType.Comma)
                {
                    token = _tokenizer.ReadToken();
                }
            }

            token.Expect(JsonTokenType.EndArray);

            yield break;
        }

        internal BsonValue ReadValue(JsonToken token)
        {
            switch (token.TokenType)
            {
                case JsonTokenType.String: return token.Token;
                case JsonTokenType.BeginDoc: return this.ReadObject();
                case JsonTokenType.BeginArray: return this.ReadArray();
                case JsonTokenType.Number:
                    return token.Token.Contains(".") ?
                        new BsonValue(Convert.ToDouble(token.Token, CultureInfo.InvariantCulture.NumberFormat)) :
                        new BsonValue(Convert.ToInt32(token.Token));
                case JsonTokenType.Word:
                    switch (token.Token)
                    {
                        case "null": return BsonValue.Null;
                        case "true": return true;
                        case "false": return false;
                        default: throw LiteException.UnexpectedToken(token.Token);
                    }
            }

            throw LiteException.UnexpectedToken(token.Token);
        }

        private BsonValue ReadObject()
        {
            var obj = new BsonDocument();

            var token = _tokenizer.ReadToken(); // read "<key>"

            while (token.TokenType != JsonTokenType.EndDoc)
            {
                token.Expect(JsonTokenType.String, JsonTokenType.Word);

                var key = token.Token;

                token = _tokenizer.ReadToken(); // read ":"

                token.Expect(JsonTokenType.Colon);

                token = _tokenizer.ReadToken(); // read "<value>"

                // check if not a special data type - only if is first attribute
                if (key[0] == '$' && obj.Count == 0)
                {
                    var val = this.ReadExtendedDataType(key, token.Token);

                    // if val is null then it's not a extended data type - it's just a object with $ attribute
                    if (!val.IsNull) return val;
                }

                obj[key] = this.ReadValue(token); // read "," or "}"

                token = _tokenizer.ReadToken();

                if (token.TokenType == JsonTokenType.Comma)
                {
                    token = _tokenizer.ReadToken(); // read "<key>"
                }
            }

            return obj;
        }

        private BsonArray ReadArray()
        {
            var arr = new BsonArray();

            var token = _tokenizer.ReadToken();

            while (token.TokenType != JsonTokenType.EndArray)
            {
                var value = this.ReadValue(token);

                arr.Add(value);

                token = _tokenizer.ReadToken();

                if (token.TokenType == JsonTokenType.Comma)
                {
                    token = _tokenizer.ReadToken();
                }
            }

            return arr;
        }

        private BsonValue ReadExtendedDataType(string key, string value)
        {
            BsonValue val;

            switch (key)
            {
                case "$binary": val = new BsonValue(Convert.FromBase64String(value)); break;
                case "$oid": val = new BsonValue(new ObjectId(value)); break;
                case "$guid": val = new BsonValue(new Guid(value)); break;
                case "$date": val = new BsonValue(DateTime.Parse(value).ToLocalTime()); break;
                case "$numberLong": val = new BsonValue(Convert.ToInt64(value)); break;
                case "$numberDecimal": val = new BsonValue(Convert.ToDecimal(value)); break;
                case "$minValue": val = BsonValue.MinValue; break;
                case "$maxValue": val = BsonValue.MaxValue; break;

                default: return BsonValue.Null; // is not a special data type
            }

            _tokenizer.ReadToken().Expect(JsonTokenType.EndDoc);

            return val;
        }
    }
}