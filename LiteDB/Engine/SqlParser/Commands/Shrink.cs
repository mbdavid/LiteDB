using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// SHRINK
        /// </summary>
        private BsonDataReader ParseShrink()
        {
            var token = _tokenizer.ReadToken().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            var result = _engine.Shrink();

            return new BsonDataReader(result);
        }
    }
}