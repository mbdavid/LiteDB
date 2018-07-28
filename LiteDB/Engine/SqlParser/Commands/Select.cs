using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        ///    SELECT [ ALL ] {selectExpr}
        ///    [ INTO {newcollection|FILE} [ : {type} ] ]
        ///    [ FROM {collection|FILE} ]
        /// [ INCLUDE {pathExpr0} [, {pathExprN} ]
        ///   [ WHERE {filterExpr} ]
        ///   [ GROUP BY {groupByExpr} [ ASC | DESC ] ]
        ///  [ HAVING {filterExpr} ]
        ///   [ ORDER BY {orderByExpr} [ ASC | DESC ] ]
        ///   [ LIMIT {number} ]
        ///  [ OFFSET {number} ]
        ///     [ FOR UPDATE ]
        /// </summary>
        private BsonDataReader ParseSelect(bool explain)
        {
            var token = _tokenizer.LookAhead();
            var all = false;

            if (token.Is("ALL"))
            {
                all = true;
                _tokenizer.ReadToken();
            }

            // read required SELECT <expr>
            var selectExpr = BsonExpression.Create(_tokenizer, _parameters);
            object into = null;
            var autoId = BsonAutoId.ObjectId;

            // read FROM|INTO
            var from = _tokenizer.ReadToken();

            if (from.Type == TokenType.EOF || from.Type == TokenType.SemiColon)
            {
                // select with no FROM - just run expression (avoid DUAL table, Mr. Oracle)
                var result = selectExpr.Execute(true);

                return new BsonDataReader(result, null);
            }
            else if (from.Is("INTO"))
            {
                into = this.ParseCollection();

                autoId = this.ParseWithAutoId();

                _tokenizer.ReadToken().Expect("FROM");
            }
            else
            {
                from.Expect("FROM");
            }

            // read FROM <name>
            var collection = this.ParseCollection();

            // initialize query builder
            QueryBuilder query;

            if (collection is string)
            {
                query = _engine.Query((string)collection);
            }
            else
            {
                query = _engine.Query((IFileCollection)collection);
            }

            // apply SELECT
            query = query.Select(selectExpr, all);

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

                ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

                if (ahead.Is("HAVING"))
                {
                    // read HAVING keyword
                    _tokenizer.ReadToken();

                    var having = BsonExpression.Create(_tokenizer, _parameters);

                    query.Having(having);
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
            if (into != null && explain == false)
            {
                var result = 0;

                if (into is string)
                {
                    result = query.Into((string)into, autoId);

                }
                else
                {
                    result = query.Into((IFileCollection)into);
                }

                return new BsonDataReader(result);
            }
            else
            {
                return query.ExecuteQuery(explain);
            }
        }

        /// <summary>
        /// Read collection name OR FILE implementations
        /// </summary>
        private object ParseCollection()
        {
            var collection = _tokenizer.ReadToken().Expect(TokenType.Word);
            var next = _tokenizer.LookAhead();

            // simple collection name
            if (next.Type != TokenType.OpenParenthesis)
            {
                return collection.Value;
            }

            // if contains ( is an FileCollection
            _tokenizer.ReadToken();

            var filename = _tokenizer.ReadToken().Expect(TokenType.String).Value;
            BsonValue options = null;

            next = _tokenizer.ReadToken();

            // if contains , read options as BsonValue
            if (next.Type == TokenType.Comma)
            {
                options = new JsonReader(_tokenizer).Deserialize();
            }

            next.Expect(TokenType.CloseParenthesis);

            // do switch to load correct FileCollection
            switch(collection.Value.ToUpper())
            {
                case "FILE_JSON": return new JsonFileCollection(filename);
                case "FILE_TEXT": return new TextFileCollection(filename);
                case "FILE_CSV": throw new NotImplementedException();
                case "FILE_BINARY": return new BinaryFileCollection(filename);
                case "FILE":
                    // auto-detect file handler based on file extension
                    switch (Path.GetExtension(filename).ToLower())
                    {
                        case ".json": return new JsonFileCollection(filename);
                        case ".txt": return new TextFileCollection(filename);
                        case ".csv": throw new NotImplementedException();
                        default: return new BinaryFileCollection(filename);
                    }
                default: throw LiteException.UnexpectedToken(collection, "Invalid collection handler");
            }
        }
    }
}