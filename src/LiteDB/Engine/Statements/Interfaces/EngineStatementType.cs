namespace LiteDB.Engine;

internal enum EngineStatementType
{
    // dml
    Insert,
    Update, 
    Delete,

    // query
    Select,

    // collections
    CreateCollection,
    RenameCollection,
    DropCollection,

    // indexes
    CreateIndex,
    DropIndex,

    // pragmas
    Pragma,

    // tools
    Checkpoint,
    Rebuild

}
