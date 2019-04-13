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
            var token = _tokenizer.ReadToken();
            var full = false;

            if (token.Is("FULL"))
            {
                full = true;
            }

            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon, TokenType.Word);

            var result = _engine.Checkpoint(full);

            return new BsonDataReader(result);
        }
    }
}