using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// DELETE [colname] WHERE [whereExpr];
        /// </summary>
        private BsonDataReader ParseDelete()
        {
            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            _tokenizer.ReadToken().Expect("WHERE");

            var where = BsonExpression.Create(_tokenizer, _parameters);

            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.Delete(collection, where);

            return new BsonDataReader(result);
        }
    }
}