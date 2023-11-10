namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// drop_collection::
    ///  "DROP" _ "COLLECTION" _ user_colection
    /// </summary>
    private IEngineStatement ParseDropCollection()
    {
        _tokenizer.ReadToken().Expect("COLLECTION"); // CREATE token already readed

        // create collection name
        var store = this.ParseDocumentStore();

        // expect end of statement
        _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

        return new DropCollectionStatement(store);
    }
}
