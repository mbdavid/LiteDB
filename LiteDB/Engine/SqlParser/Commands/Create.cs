using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// CREATE [UNQIUE] INDEX [name::word] ON [colname::word] ([expr]);
        /// </summary>
        private BsonDataReader ParseCreate()
        {
            var token = _tokenizer.ReadToken().Expect(TokenType.Word);
            var unique = token.Is("UNIQUE");

            if (unique)
            {
                token = _tokenizer.ReadToken();
            }

            token.Expect("INDEX");

            // read indexName
            var name = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            _tokenizer.ReadToken().Expect("ON");

            // read collectionName
            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            _tokenizer.ReadToken().Expect(TokenType.OpenParenthesis);

            var expr = BsonExpression.Create(_tokenizer, null);

            _tokenizer.ReadToken().Expect(TokenType.CloseParenthesis);

            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.EnsureIndex(collection, name, expr, unique);

            return new BsonDataReader(result);
        }
    }
}