namespace LiteDB.Engine;

/// <summary>
/// Represent a set of internal variables to control database. This data will be stored just after FILE_HEADER
/// </summary>
internal class Pragmas
{
    #region Buffer Field Positions

    public const int P_IS_DIRTY = 0;      // 00-00 [byte]
    public const int P_USER_VERSION = 1;  // 01-04 [int]
    public const int P_LIMIT_SIZE_ID = 5; // 05-08 [int]
    public const int P_CHECKPOINT = 9;    // 09-12 [int]

    // reserved 13-63

    #endregion

    /// <summary>
    /// Get/Set when datafile are changed. Set true just before first write operation and write back to false just before dispose database
    /// </summary>
    public bool IsDirty = false;

    /// <summary>
    /// Internal user version control to detect database changes
    /// </summary>
    public int UserVersion = 0;

    /// <summary>
    /// Max limit of datafile (in bytes) (default: MaxValue)
    /// </summary>
    public int LimitSizeID = 0;

    /// <summary>
    /// When LOG file gets larger than checkpoint size (in pages), do a soft checkpoint (and also do a checkpoint at shutdown)
    /// Checkpoint = 0 means there's no auto-checkpoint nor shutdown checkpoint
    /// </summary>
    public int Checkpoint = CHECKPOINT_SIZE;

    /// <summary>
    /// Create empty version of pragma variables
    /// </summary>
    public Pragmas()
    {
    }

    /// <summary>
    /// Read pragma variable from a existing buffer data
    /// </summary>
    public Pragmas(Span<byte> buffer)
    {
        this.IsDirty = buffer[P_IS_DIRTY] != 0;
        this.UserVersion = buffer[P_USER_VERSION..].ReadInt32();
        this.LimitSizeID = buffer[P_LIMIT_SIZE_ID..].ReadInt32();
        this.Checkpoint = buffer[P_CHECKPOINT..].ReadInt32();
    }

    /// <summary>
    /// Write all content into buffer
    /// </summary>
    public void Write(Span<byte> buffer)
    {
        // write flags/data into file 
        buffer[P_IS_DIRTY] = this.IsDirty ? (byte)1 : (byte)0;
        buffer[P_USER_VERSION..].WriteInt32(this.UserVersion);
        buffer[P_LIMIT_SIZE_ID..].WriteInt32((int)this.LimitSizeID);
        buffer[P_CHECKPOINT..].WriteInt32((int)this.Checkpoint);
    }

    public override string ToString()
    {
        return Dump.Object(this);
    }

}
