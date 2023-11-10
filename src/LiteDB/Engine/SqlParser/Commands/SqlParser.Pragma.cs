namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// pragma::
    ///   "PRAGMA" _ pragma. "=" . json_value
    /// </summary>
    private IEngineStatement ParsePragma()
    {
        _tokenizer.ReadToken().Expect("PRAGMA");

        var name = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

        _tokenizer.ReadToken().Expect(TokenType.Equals);

        // read <value>
        var token = _tokenizer.ReadToken().Expect(TokenType.Int);

        var value = int.Parse(token.Value);

        // read last ; \ <eof>
        _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

        return new PragmaStatement(name, value);
    }
}
