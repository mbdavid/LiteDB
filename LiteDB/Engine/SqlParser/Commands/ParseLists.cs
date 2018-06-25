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
        private IEnumerable<BsonExpression> ParseListOfExpressions()
        {
            while(true)
            {
                var expr = BsonExpression.Create(_tokenizer, _parameters);

                yield return expr;

                var next = _tokenizer.LookAhead();
                
                if (next.Type == TokenType.Comma)
                {
                    _tokenizer.ReadToken();
                }
                else
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// [value1], [value2], ..., [valueN]
        /// </summary>
        private IEnumerable<BsonDocument> ParseListOfDocuments()
        {
            var reader = new JsonReader(_tokenizer);

            while (true)
            {
                var value = reader.Deserialize();

                if (value.IsDocument)
                {
                    yield return value as BsonDocument;
                }
                else
                {
                    throw LiteException.UnexpectedToken("Value must be a valid document", _tokenizer.Current);
                }

                var next = _tokenizer.LookAhead();

                if (next.Type == TokenType.Comma)
                {
                    _tokenizer.ReadToken();
                }
                else
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// [word1], [word2], ..., [wordN]
        /// </summary>
        private IEnumerable<string> ParseListOfWords()
        {
            while (true)
            {
                var token = _tokenizer.LookAhead();

                if (token.Type == TokenType.Word)
                {
                    _tokenizer.ReadToken();

                    yield return token.Value;

                    var next = _tokenizer.LookAhead();

                    if (next.Type == TokenType.Comma)
                    {
                        _tokenizer.ReadToken();
                    }
                    else
                    {
                        yield break;
                    }
                }
                else
                {
                    yield break;
                }
            }
        }
    }
}