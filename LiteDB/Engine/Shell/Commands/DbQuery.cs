using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class ShellParser
    {
        /// <summary>
        /// db.[col].[query|count|aggregate|exists] ...
        /// </summary>
        private void DbQuery(string name, string command)
        {
            var query = _engine.Query(name);

            // read select <expr> 
            var select = BsonExpression.Create(_tokenizer, _parameters);

            query.Select(select);

            var ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("WHERE"))
            {
                _tokenizer.ReadToken();

                var where = BsonExpression.Create(_tokenizer, _parameters);

                query.Where(where);
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("GROUP"))
            {
                _tokenizer.ReadToken();
                _tokenizer.ReadToken().Expect("BY");

                var groupBy = BsonExpression.Create(_tokenizer, _parameters);

                var groupByOrder = Query.Ascending;
                var groupByToken = _tokenizer.LookAhead();

                if (groupByToken.Is("ASC") || groupByToken.Is("DESC"))
                {
                    groupByOrder = _tokenizer.ReadToken().Is("ASC") ? Query.Ascending : Query.Descending;
                }

                query.GroupBy(groupBy, groupByOrder);
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("ORDER"))
            {
                _tokenizer.ReadToken();
                _tokenizer.ReadToken().Expect("BY");

                var orderBy = BsonExpression.Create(_tokenizer, _parameters);

                var orderByOrder = Query.Ascending;
                var orderByToken = _tokenizer.LookAhead();

                if (orderByToken.Is("ASC") || orderByToken.Is("DESC"))
                {
                    orderByOrder = _tokenizer.ReadToken().Is("ASC") ? Query.Ascending : Query.Descending;
                }

                query.GroupBy(orderBy, orderByOrder);
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("LIMIT"))
            {
                _tokenizer.ReadToken();
                var limit = _tokenizer.ReadToken().Expect(TokenType.Int).Value;

                query.Limit(Convert.ToInt32(limit));
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("OFFSET"))
            {
                _tokenizer.ReadToken();
                var offset = _tokenizer.ReadToken().Expect(TokenType.Int).Value;

                query.Offset(Convert.ToInt32(offset));
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("FOR"))
            {
                _tokenizer.ReadToken();
                _tokenizer.ReadToken().Expect("UPDATE");

                query.ForUpdate();
            }

            this.WriteResult(query.ToEnumerable());
        }
    }
}