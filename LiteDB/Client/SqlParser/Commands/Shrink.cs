using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB.Engine;
using static LiteDB.Constants;

namespace LiteDB
{
    internal partial class SqlParser
    {
        /// <summary>
        /// SHRINK
        /// </summary>
        private BsonDataReader ParseShrink()
        {
            _tokenizer.ReadToken().Expect("SHRINK");

            // read <eol> or ;
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var diff = _engine.Shrink();

            return new BsonDataReader((int)diff);
        }
    }
}