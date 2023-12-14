namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// delete_statement::
    ///     "DELETE" _ user_collection
    ///  [_ "WHERE" _ expr_predicate]
    /// </summary>
    private IEngineStatement ParseDelete()
    {
        _tokenizer.ReadToken(); // read DELETE

        var collectionName = this.ParseUserCollection();

        var ahead = _tokenizer.LookAhead().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

        if (ahead.Match("WHERE"))
        {
            // read WHERE keyword
            _tokenizer.ReadToken();

            var where = BsonExpression.Create(_tokenizer, true);

            // expect end of statement
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            return new DeleteStatement(collectionName, where);
        }
        else
        {
            return new DeleteStatement(collectionName, BsonExpression.Empty);
        }
    }
}
