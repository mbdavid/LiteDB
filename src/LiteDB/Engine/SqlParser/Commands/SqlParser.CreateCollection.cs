namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// create_collection::
    ///  "CREATE" _ "COLLECTION" _ user_collection
    /// </summary>
    private IEngineStatement ParseCreateCollection()
    {
        _tokenizer.ReadToken().Expect("COLLECTION"); // CREATE token already readed

        // create collection name
        if (!this.TryParseDocumentStore(out var store)) throw ERR_UNEXPECTED_TOKEN(_tokenizer.Current, "{document_store}");

        // expect end of statement
        _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

        return new CreateCollectionStatement(store.Name);
    }
}
