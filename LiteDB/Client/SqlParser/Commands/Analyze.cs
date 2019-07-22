using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB.Engine;

namespace LiteDB
{
    internal partial class SqlParser
    {
        /// <summary>
        /// ANALYZE [{collection0}, {collection1}, ...]
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