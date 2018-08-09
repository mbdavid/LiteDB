using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// UPDATE - will merge current document with modify expression
        ///  UPDATE {collection}
        ///     SET {extendExpr}
        /// [ WHERE {whereExpr} ]
        /// </summary>
        private BsonDataReader ParseUpadate()
        {
            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;
            _tokenizer.ReadToken().Expect("SET");
            var extend = BsonExpression.Create(_tokenizer, _parameters);

            // optional where
            BsonExpression where = null;
            var token = _tokenizer.LookAhead();

            if (token.Is("WHERE"))
            {
                // read WHERE
                _tokenizer.ReadToken();

                where = BsonExpression.Create(_tokenizer, _parameters);
            }

            // read eof
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.Update(collection, extend, where);

            return new BsonDataReader(result);
        }
    }
}