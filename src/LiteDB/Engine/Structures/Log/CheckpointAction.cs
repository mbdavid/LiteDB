namespace LiteDB.Engine;

internal struct CheckpointAction
{
    public CheckpointActionType Action;
    public uint PositionID;
    public uint TargetPositionID; // used only in CopyToDataFile and CopyToTemp (MaxValue for ClearPage)
    public bool MustClear; // clear page PositionID

    public override string ToString()
    {
        return Dump.Object(new { Action, PositionID = Dump.PageID(PositionID), TargetPositionID = Dump.PageID(TargetPositionID), MustClear });
    }
}