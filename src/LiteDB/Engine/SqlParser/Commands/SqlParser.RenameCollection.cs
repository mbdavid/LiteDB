namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// rename_collection::
    ///   "RENAME" _ "COLLECTION" _ user_collection _ "TO" _ user_collection
    /// </summary>
    private IEngineStatement ParseRenameCollection()
    {
        _tokenizer.ReadToken(); // read RENAME
        _tokenizer.ReadToken().Expect("COLLECTION");

        // get current collection name
        var store = this.ParseDocumentStore();

        _tokenizer.ReadToken().Expect("TO");

        // read new name as a word
        var newName = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

        // expect end of statement
        _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

        return new RenameCollectionStatement(store, newName);
    }
}
