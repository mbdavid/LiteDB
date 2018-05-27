using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class ShellParser
    {
        /// <summary>
        /// db.[colname].drop
        /// </summary>
        private void DbDrop(string name)
        {
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.DropCollection(name);

            this.WriteSingle(result);
        }
    }
}