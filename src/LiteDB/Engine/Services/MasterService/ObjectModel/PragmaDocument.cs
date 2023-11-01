namespace LiteDB.Engine;

internal class PragmaDocument
{
    /// <summary>
    /// Internal user version control to detect database changes
    /// </summary>
    public int UserVersion { get; set; } = 0;

    /// <summary>
    /// Max limit of datafile (in bytes) (default: MaxValue)
    /// </summary>
    public int LimitSizeID { get; set; } = 0;

    /// <summary>
    /// When LOG file gets larger than checkpoint size (in pages), do a soft checkpoint (and also do a checkpoint at shutdown)
    /// Checkpoint = 0 means there's no auto-checkpoint nor shutdown checkpoint
    /// </summary>
    public int Checkpoint { get; set; } = CHECKPOINT_SIZE;

    /// <summary>
    /// Initial pragma values
    /// </summary>
    public PragmaDocument()
    {
    }

    /// <summary>
    /// Clone object instance constructor
    /// </summary>
    public PragmaDocument(PragmaDocument other)
    {
        this.UserVersion = other.UserVersion;
        this.LimitSizeID = other.LimitSizeID;
        this.Checkpoint = other.Checkpoint;
    }
}

