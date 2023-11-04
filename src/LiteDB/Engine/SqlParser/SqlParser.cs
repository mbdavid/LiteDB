namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    private Tokenizer _tokenizer;
    private Collation _collation;
    private int _nameIndex = 0; // used for columns with no names will generate "expr1", "expr2"

    public SqlParser(Tokenizer tokenizer, Collation collation)
    {
        _tokenizer = tokenizer;
        _collation = collation;
    }

    public IEngineStatement ParseStatement()
    {
        var ahead = _tokenizer.LookAhead().Expect(TokenType.Word);

        if (ahead.Match("CREATE"))
        {
            _tokenizer.ReadToken(); // read CREATE
            ahead = _tokenizer.LookAhead();

            if (ahead.Match("COLLECTION")) return this.ParseCreateCollection();
            if (ahead.Match("INDEX")) return this.ParseCreateIndex();

            throw ERR_UNEXPECTED_TOKEN(ahead);
        }

        if (ahead.Match("DROP"))
        {
            _tokenizer.ReadToken(); // read DROP
            ahead = _tokenizer.LookAhead();

            if (ahead.Match("COLLECTION")) return this.ParseDropCollection();
            if (ahead.Match("INDEX")) return this.ParseDropIndex();

            throw ERR_UNEXPECTED_TOKEN(ahead);
        }

        if (ahead.Match("SELECT") || ahead.Match("EXPLAIN")) return this.ParseSelect();

        if (ahead.Match("INSERT")) return this.ParseInsert();

        if (ahead.Match("DELETE")) return this.ParseDelete();

        //if (ahead.Match("UPDATE")) return this.ParseUpdate();
        //if (ahead.Match("RENAME")) return this.ParseRename();
        //
        if (ahead.Match("CHECKPOINT")) return this.ParseCheckpoint();
        //if (ahead.Match("REBUILD")) return this.ParseRebuild();

        //if (ahead.Value.Eq("PRAGMA")) return this.ParsePragma();

        throw ERR_UNEXPECTED_TOKEN(ahead);
    }
}