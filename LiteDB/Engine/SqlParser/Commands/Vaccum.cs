using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// VACCUM
        /// </summary>
        private BsonDataReader ParseVaccum()
        {
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.Vaccum();

            return new BsonDataReader(result);
        }
    }
}