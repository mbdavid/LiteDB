using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class ShellParser
    {
        /// <summary>
        /// db.param userVersion
        /// db.param userVersion = [value]
        /// </summary>
        private void DbParam()
        {
            var paramName = _tokenizer.ReadToken().Expect(TokenType.Word).Value;
            var equals = _tokenizer.ReadToken().Expect(TokenType.Equals, TokenType.EOF, TokenType.SemiColon);

            if (equals.Type == TokenType.Equals)
            {
                var expr = BsonExpression.Create(_tokenizer, _parameters);

                // after read expression must EOF or ;
                _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

                var value = expr.Execute().FirstOrDefault();

                var result = _engine.SetParameter(paramName, value);

                this.WriteSingle(result);
            }
            else
            {
                var value = _engine.GetParameter(paramName, BsonValue.Null);

                this.WriteSingle(value);
            }
        }
    }
}