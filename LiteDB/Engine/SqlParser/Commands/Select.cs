using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        ///    SELECT [ AGGREGATE ] {selectExpr}
        ///    [ INTO {newcollection} [ : {type} ] ]
        ///    [ FROM {collection} ]
        /// [ INCLUDE {pathExpr0} [, {pathExprN} ]
        ///   [ WHERE {whereExpr} ]
        /// [ INCLUDE {pathExpr0} [, {pathExprN} ]
        ///   [ GROUP BY {groupByExpr} [ ASC | DESC ] ]
        ///   [ ORDER BY {orderByExpr} [ ASC | DESC ] ]
        ///   [ LIMIT {number} ]
        ///  [ OFFSET {number} ]
        ///     [ FOR UPDATE ]
        /// </summary>
        private BsonDataReader ParseSelect(bool explainPlan)
        {
            var token = _tokenizer.LookAhead();
            var aggregate = false;

            if (token.Is("ALL"))
            {
                aggregate = true;
                _tokenizer.ReadToken();
            }

            // read required SELECT <expr>
            var selectExpr = BsonExpression.Create(_tokenizer, _parameters);
            string into = null;
            var autoId = BsonAutoId.ObjectId;

            // read FROM|INTO
            var from = _tokenizer.ReadToken();

            if (from.Type == TokenType.EOF || from.Type == TokenType.SemiColon)
            {
                // select with no FROM - just run expression (avoid DUAL table, Mr. Oracle)
                var result = selectExpr.Execute(true).First();

                return new BsonDataReader(result);
            }
            else if (from.Is("INTO"))
            {
                into = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

                autoId = this.ParseWithAutoId();

                _tokenizer.ReadToken().Expect("FROM");
            }
            else
            {
                from.Expect("FROM");
            }

            // read FROM <name>
            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            // initialize query builder
            var query = _engine.Query(collection)
                .Select(selectExpr, aggregate);

            var ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("INCLUDE"))
            {
                // read first INCLUDE (before)
                _tokenizer.ReadToken();

                foreach(var path in this.ParseListOfExpressions())
                {
                    query.Include(path);
                }
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("WHERE"))
            {
                // read WHERE keyword
                _tokenizer.ReadToken();

                var where = BsonExpression.Create(_tokenizer, _parameters);

                query.Where(where);
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("GROUP"))
            {
                // read GROUP BY keyword
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

            if (ahead.Is("INCLUDE"))
            {
                // read second INCLUDE (after)
                _tokenizer.ReadToken();

                foreach (var path in this.ParseListOfExpressions())
                {
                    query.Include(path);
                }
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("ORDER"))
            {
                // read ORDER BY keyword
                _tokenizer.ReadToken();
                _tokenizer.ReadToken().Expect("BY");

                var orderBy = BsonExpression.Create(_tokenizer, _parameters);

                var orderByOrder = Query.Ascending;
                var orderByToken = _tokenizer.LookAhead();

                if (orderByToken.Is("ASC") || orderByToken.Is("DESC"))
                {
                    orderByOrder = _tokenizer.ReadToken().Is("ASC") ? Query.Ascending : Query.Descending;
                }

                query.OrderBy(orderBy, orderByOrder);
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("LIMIT"))
            {
                // read LIMIT keyword
                _tokenizer.ReadToken();
                var limit = _tokenizer.ReadToken().Expect(TokenType.Int).Value;

                query.Limit(Convert.ToInt32(limit));
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("OFFSET"))
            {
                // read OFFSET keyword
                _tokenizer.ReadToken();
                var offset = _tokenizer.ReadToken().Expect(TokenType.Int).Value;

                query.Offset(Convert.ToInt32(offset));
            }

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Is("FOR"))
            {
                // read FOR keyword
                _tokenizer.ReadToken();
                _tokenizer.ReadToken().Expect("UPDATE");

                query.ForUpdate();
            }

            // read eof/;
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            // execute query as insert or return values
            if (into != null && explainPlan == false)
            {
                var result = query.Into(into, autoId);

                return new BsonDataReader(into);
            }
            else
            {
                return query.ExecuteReader(explainPlan);
            }
        }
    }
}