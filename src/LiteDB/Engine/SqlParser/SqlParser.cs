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

        if (ahead.Value.Eq("CREATE"))
        {
            _tokenizer.ReadToken(); // read CREATE
            ahead = _tokenizer.LookAhead();

            if (ahead.Value.Eq("COLLECTION")) return this.ParseCreateCollection();
            if (ahead.Value.Eq("INDEX")) return this.ParseCreateIndex();

            throw ERR_UNEXPECTED_TOKEN(ahead);
        }


        if (ahead.Value.Eq("SELECT") || ahead.Value.Eq("EXPLAIN")) return this.ParseSelect();

        if (ahead.Value.Eq("INSERT")) return this.ParseInsert();

        //if (ahead.Value.Eq("DELETE")) return this.ParseDelete();
        //if (ahead.Value.Eq("UPDATE")) return this.ParseUpdate();
        //if (ahead.Value.Eq("DROP")) return this.ParseDrop();
        //if (ahead.Value.Eq("RENAME")) return this.ParseRename();
        //if (ahead.Value.Eq("CREATE")) return this.ParseCreate();
        //
        //if (ahead.Value.Eq("CHECKPOINT")) return this.ParseCheckpoint();
        //if (ahead.Value.Eq("REBUILD")) return this.ParseRebuild();
        //
        //if (ahead.Value.Eq("BEGIN")) return this.ParseBegin();
        //if (ahead.Value.Eq("ROLLBACK")) return this.ParseRollback();
        //if (ahead.Value.Eq("COMMIT")) return this.ParseCommit();
        //
        //if (ahead.Value.Eq("PRAGMA")) return this.ParsePragma();

        throw ERR_UNEXPECTED_TOKEN(ahead);
    }
}