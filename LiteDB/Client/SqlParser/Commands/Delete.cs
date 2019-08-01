using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB.Engine;
using static LiteDB.Constants;

namespace LiteDB
{
    internal partial class SqlParser
    {
        /// <summary>
        /// DELETE {collection} WHERE {whereExpr}
        /// </summary>
        private BsonDataReader ParseDelete()
        {
            _tokenizer.ReadToken().Expect("DELETE");

            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            _tokenizer.ReadToken().Expect("WHERE");

            var where = BsonExpression.Create(_tokenizer, _parameters, BsonExpressionParserMode.Full);

            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.DeleteMany(collection, where);

            return new BsonDataReader(result);
        }
    }
}