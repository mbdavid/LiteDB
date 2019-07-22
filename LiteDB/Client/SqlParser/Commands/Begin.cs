using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB.Engine;

namespace LiteDB
{
    internal partial class SqlParser
    {
        /// <summary>
        /// BEGIN [ TRANS | TRANSACTION ]
        /// </summary>
        private BsonDataReader ParseBegin()
        {
            var token = _tokenizer.ReadToken().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (token.Is("TRANS") || token.Is("TRANSACTION"))
            {
                _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);
            }

            var transactionId = _engine.BeginTrans();

            return new BsonDataReader(transactionId);
        }
    }
}