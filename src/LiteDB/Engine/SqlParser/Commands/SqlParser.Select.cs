namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// select_statement::
    ///      "SELECT" [ _ "DISTINCT" ] _ {select-fields}
    ///    [ _ "INTO" _ document_store [ ":" auto_id ] ] // remover? usar o INSERT INTO com sub query
    ///    [ _ "FROM" _ document_store
    /// [ _ "INCLUDE" _ expr_single [ . "," . expr_single ]* ]
    ///   [ _ "WHERE" _ expr_predicate ]
    ///   [ _ "GROUP" _ "BY" _ expr_single
    ///  [ _ "HAVING" _ expr_predicate ] ]
    ///   [ _ "ORDER" _ "BY" _ expr_single [ _ ("ASC" | "DESC") ] ]
    ///   [ _ "LIMIT" _ (json_number | expr_parameter) ]
    ///  [ _ "OFFSET" _ (json_number | expr_parameter) ] ] // close optional FROM
    /// </summary>
    private IEngineStatement ParseSelect()
    {
        // all query fields
        SelectFields _select;
        var _distinct = false;
        var _into = Into.Empty;
        var _includes = (IReadOnlyList<BsonExpression>)Array.Empty<BsonExpression>();
        var _where = BsonExpression.Empty;
        var _groupBy = BsonExpression.Empty;
        var _having = BsonExpression.Empty;
        var _orderBy = OrderBy.Empty;
        var _offset = 0;
        var _limit = int.MaxValue;

        var explain = _tokenizer.LookAhead().Match("EXPLAIN");

        if (explain) _tokenizer.ReadToken();

        _tokenizer.ReadToken().Expect("SELECT");

        _distinct = _tokenizer.LookAhead().Match("DISTINCT");

        if (_distinct) _tokenizer.ReadToken();

        _select = this.ParseSelectFields();

        // read FROM|INTO
        var from = _tokenizer.ReadToken();

        if (from.Type == TokenType.EOF || from.Type == TokenType.SemiColon)
        {
            throw new NotImplementedException();
        }
        else if (from.Match("INTO"))
        {
            if (!this.TryParseDocumentStore(out var intoStore)) throw ERR_UNEXPECTED_TOKEN(_tokenizer.Current, "document-store::");

            this.TryParseWithAutoId(out var intoAutoId);

            _into = new Into(intoStore.Name, intoAutoId);

            _tokenizer.ReadToken().Expect("FROM");
        }
        else
        {
            from.Expect("FROM");
        }

        // read FROM <name>
        if (!this.TryParseDocumentStore(out var fromStore)) throw ERR_UNEXPECTED_TOKEN(_tokenizer.Current, "document-store::");

        var ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

        if (ahead.Match("INCLUDE"))
        {
            // read first INCLUDE (before)
            _tokenizer.ReadToken();

            _includes = this.ParseListOfExpressions().ToArray();
        }

        ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

        if (ahead.Match("WHERE"))
        {
            // read WHERE keyword
            _tokenizer.ReadToken();

            _where = BsonExpression.Create(_tokenizer, true);
        }

        ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

        if (ahead.Match("GROUP"))
        {
            // read GROUP BY keyword
            _tokenizer.ReadToken();
            _tokenizer.ReadToken().Expect("BY");

            _groupBy = BsonExpression.Create(_tokenizer, false);

            ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            if (ahead.Match("HAVING"))
            {
                // read HAVING keyword
                _tokenizer.ReadToken();

                _having = BsonExpression.Create(_tokenizer, true);
            }
        }

        ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

        if (ahead.Match("ORDER"))
        {
            // read ORDER BY keyword
            _tokenizer.ReadToken();
            _tokenizer.ReadToken().Expect("BY");

            var orderByExpr = BsonExpression.Create(_tokenizer, true);
            var orderByOrder = Query.Ascending;

            ahead = _tokenizer.LookAhead();

            if (ahead.Match("ASC") || ahead.Match("DESC"))
            {
                orderByOrder = _tokenizer.ReadToken().Match("ASC") ? Query.Ascending : Query.Descending;
            }

            _orderBy = new OrderBy(orderByExpr, orderByOrder);
        }

        ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

        if (ahead.Match("LIMIT"))
        {
            // read LIMIT keyword
            _tokenizer.ReadToken();

            _limit = int.Parse(_tokenizer.ReadToken().Expect(TokenType.Int).Value);
        }

        ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

        if (ahead.Match("OFFSET"))
        {
            // read OFFSET keyword
            _tokenizer.ReadToken();

            _offset = int.Parse(_tokenizer.ReadToken().Expect(TokenType.Int).Value);
        }

        // read eof/;
        _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

        // create query object instance
        var query = new Query
        {
            Collection = fromStore.Name,
            Select = _select,
            Distinct = _distinct,
            Into = _into,
            Includes = _includes,
            Where = _where,
            GroupBy = _groupBy,
            Having = _having,
            OrderBy = _orderBy,
            Limit = _limit,
            Offset = _offset,
        };

        return new SelectStatement(query, explain);
    }
}
