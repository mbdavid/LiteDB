namespace LiteDB.Engine;

internal enum CheckpointActionType : byte
{
    CopyToDataFile = 0,
    CopyToTempFile = 1,
    ClearPage = 2
}
