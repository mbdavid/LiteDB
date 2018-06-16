using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// ANALYZE [col1, col2, ...]
        /// </summary>
        private BsonDataReader ParseAnalyze()
        {
            var cols = this.ParseListOfWords().ToArray();

            // read eof/;
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.Analyze(cols);

            return new BsonDataReader(result);
        }
    }
}