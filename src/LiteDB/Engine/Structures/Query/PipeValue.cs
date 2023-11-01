namespace LiteDB.Engine;

internal readonly struct PipeValue : IIsEmpty
{
    public readonly RowID IndexNodeID;
    public readonly RowID DataBlockID;
    public readonly BsonValue Value;

    public static readonly PipeValue Empty = new();

    public readonly bool IsEmpty => this.IndexNodeID.IsEmpty && this.DataBlockID.IsEmpty && this.Value.IsNull;

    public PipeValue(RowID indexNodeID, RowID dataBlockID)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Value = BsonValue.Null;
    }

    public PipeValue(RowID indexNodeID, RowID dataBlockID, BsonValue value)
    {
        this.IndexNodeID = indexNodeID;
        this.DataBlockID = dataBlockID;
        this.Value = value;
    }

    public PipeValue(BsonValue value)
    {
        this.IndexNodeID = RowID.Empty;
        this.DataBlockID = RowID.Empty;
        this.Value = value;
    }

    public PipeValue()
    {
        this.IndexNodeID = RowID.Empty;
        this.DataBlockID = RowID.Empty;
        this.Value = BsonValue.Null;
    }

    public override string ToString() => Dump.Object(this);
}
