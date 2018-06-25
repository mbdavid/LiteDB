using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// SET {key} = {value}
        /// </summary>
        private BsonDataReader ParseSet()
        {
            var key = _tokenizer.ReadToken().Expect(TokenType.Word);

            _tokenizer.ReadToken().Expect(TokenType.Equals);

            var reader = new JsonReader(_tokenizer);

            var value = reader.Deserialize();

            // read eof/;
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            switch (key.Value.ToLower())
            {
                case "userversion":
                    _engine.UserVersion = value.AsInt32;
                    break;

                default:
                    throw LiteException.UnexpectedToken("Unkown key value", key);
            }

            return new BsonDataReader();
        }
    }
}