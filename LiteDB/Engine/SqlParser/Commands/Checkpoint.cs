using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// checkpoint
        /// </summary>
        private BsonDataReader ParseCheckpoint()
        {
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            _engine.Checkpoint(false);

            return new BsonDataReader(null);
        }
    }
}