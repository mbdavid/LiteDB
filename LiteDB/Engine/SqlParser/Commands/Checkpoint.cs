using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// CHECKPOINT [DELETE]
        /// </summary>
        private BsonDataReader ParseCheckpoint()
        {
            var token = _tokenizer.ReadToken();
            var delete = token.Is("DELETE");

            if (delete)
            {
                _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);
            }
            else
            {
                token.Expect(TokenType.EOF, TokenType.SemiColon);
            }

            var result = _engine.Checkpoint(delete);

            return new BsonDataReader(result);
        }
    }
}