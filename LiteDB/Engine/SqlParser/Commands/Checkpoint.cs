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
            var mode = CheckpointMode.Incremental;

            if (token.Is("FULL"))
            {
                mode = CheckpointMode.Full;
            }

            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon, TokenType.Word);

            var result = _engine.Checkpoint(mode);

            return new BsonDataReader(result);
        }
    }
}