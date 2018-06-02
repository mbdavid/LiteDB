using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class ShellParser
    {
        /// <summary>
        /// db.[colname].ensureIndex [name] unique using [expr]
        /// </summary>
        private void DbEnsureIndex(string name)
        {
            var indexName = _tokenizer.ReadToken().Expect(TokenType.Word).Value;
            var expr = BsonExpression.Create(indexName);
            var unique = false;

            var token = _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.Word);

            if (token.Is("UNIQUE"))
            {
                unique = true;
                token = _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.Word);
            }

            if (token.Is("USING"))
            {
                expr = BsonExpression.Create(_tokenizer, null);
            }
            else if(token.Type != TokenType.EOF && token.Type != TokenType.SemiColon)
            {
                throw LiteException.UnexpectedToken(token);
            }

            var result = _engine.EnsureIndex(name, indexName, expr, unique);

            this.WriteSingle(result);
        }
    }
}