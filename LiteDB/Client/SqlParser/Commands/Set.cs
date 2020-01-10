using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB.Engine;
using static LiteDB.Constants;

namespace LiteDB
{
    internal partial class SqlParser
    {
        /// <summary>
        /// SET [DB_PARAM] = VALUE
        /// SET [DB_PARAM]
        /// </summary>
        private IBsonDataReader ParseSet()
        {
            _tokenizer.ReadToken().Expect("SET");

            var name = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            var eof = _tokenizer.LookAhead();

            if (eof.Type == TokenType.EOF || eof.Type == TokenType.SemiColon)
            {
                _tokenizer.ReadToken();

                var result = _engine.DbParam(name);

                return new BsonDataReader(result);
            }
            else if (eof.Type == TokenType.Equals)
            {
                _tokenizer.ReadToken().Expect(TokenType.Equals);

                var reader = new JsonReader(_tokenizer);
                var value = reader.Deserialize();
                var result = _engine.DbParam(name, value);

                return new BsonDataReader(result);
            }

            throw LiteException.UnexpectedToken(eof);
        }
    }
}