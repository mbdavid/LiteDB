namespace LiteDB.Engine;

/// <summary>
/// Structure to define enumerators emit after pipe (indexBlockID, dataBlockID and/or value)
/// </summary>
internal readonly struct PipeEmit
{
    public readonly bool IndexNodeID;
    public readonly bool DataBlockID;
    public readonly bool Value;

    public PipeEmit(bool indexNodeID, bool dataBlockID, bool value)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Value = value;
    }

    public override string ToString() => Dump.Object(this);
}
