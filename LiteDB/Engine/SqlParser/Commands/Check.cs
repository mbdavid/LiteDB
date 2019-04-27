using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// CHECK INTEGRITY
        /// </summary>
        private BsonDataReader ParseCheck()
        {
            _tokenizer.ReadToken().Expect("INTEGRITY");
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var report = _engine.CheckIntegrity();

            return new BsonDataReader(new BsonArray(report.Summary.Select(x => new BsonValue(x))));
        }
    }
}