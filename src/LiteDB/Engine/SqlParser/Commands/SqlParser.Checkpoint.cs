namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// checkpoint::
    ///  "CHECKPOINT"
    /// </summary>
    private IEngineStatement ParseCheckpoint()
    {
        _tokenizer.ReadToken(); // read CHECKPOINT

        return new CheckpointStatement();
    }
}
