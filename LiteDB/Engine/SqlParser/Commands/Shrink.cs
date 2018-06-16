using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// SHRINK [new-password]
        /// </summary>
        private BsonDataReader ParseShrink()
        {
            var token = _tokenizer.ReadToken().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);
            string password = null;

            if (token.Type == TokenType.Word)
            {
                password = token.Value;

                _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);
            }

            var result = _engine.Shrink(password);

            return new BsonDataReader(result);
        }
    }
}