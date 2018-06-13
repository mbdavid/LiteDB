using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// [expr1], [expr2], ..., [exprN]
        /// </summary>
        private List<BsonExpression> ParseListOfExpressions()
        {
            var result = new List<BsonExpression>();

            while(true)
            {
                var expr = BsonExpression.Create(_tokenizer, _parameters);

                result.Add(expr);

                var next = _tokenizer.LookAhead();
                
                if (next.Type == TokenType.Comma)
                {
                    _tokenizer.ReadToken();
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// [value1], [value2], ..., [valueN]
        /// </summary>
        private List<BsonValue> ParseListOfValues()
        {
            var result = new List<BsonValue>();
            var reader = new JsonReader(_tokenizer);

            while (true)
            {
                var value = reader.Deserialize();

                result.Add(value);

                var next = _tokenizer.LookAhead();

                if (next.Type == TokenType.Comma)
                {
                    _tokenizer.ReadToken();
                }
                else
                {
                    break;
                }
            }

            return result;
        }
    }
}