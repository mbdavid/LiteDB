using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        ///  UPDATE [collection]
        ///     SET [modifyExpr]
        ///   WHERE [whereExpr]
        /// </summary>
        private BsonDataReader ParseUpadate()
        {
            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;
            _tokenizer.ReadToken().Expect("SET");
            var modify = BsonExpression.Create(_tokenizer, null);

            // optional where
            BsonExpression where = null;
            var token = _tokenizer.LookAhead();

            if (token.Is("WHERE"))
            {
                _tokenizer.ReadToken();
                where = BsonExpression.Create(_tokenizer, null);
            }
            else
            {
                token.Expect(TokenType.EOF, TokenType.SemiColon);
            }

            var result = _engine.Update(collection, modify, where);

            return new BsonDataReader(result);
        }
    }
}