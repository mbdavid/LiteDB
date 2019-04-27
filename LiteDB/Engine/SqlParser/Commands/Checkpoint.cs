using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// CHECKPOINT
        /// </summary>
        private BsonDataReader ParseCheckpoint()
        {
            // read CHECKPOINT
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            _engine.Checkpoint();

            return new BsonDataReader();
        }
    }
}