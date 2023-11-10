namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// drop_index::
    ///   "DROP" _ "INDEX" _ user_collection "." word
    /// </summary>
    private IEngineStatement ParseDropIndex()
    {
        _tokenizer.ReadToken().Expect("INDEX"); // CREATE token already readed

        // create collection name
        var store = this.ParseDocumentStore();

        // expect .
        _tokenizer.ReadToken().Expect(TokenType.Period);

        var indexName = _tokenizer.ReadToken().Expect(TokenType.Word);

        // expect end of statement
        _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

        return new DropIndexStatement(store, indexName.Value);
    }
}
