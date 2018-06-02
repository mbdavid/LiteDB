using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class ShellParser
    {
        /// <summary>
        /// db.checkpoint
        /// </summary>
        private void DbCheckpoint()
        {
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            _engine.Checkpoint();
        }
    }
}