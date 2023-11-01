namespace LiteDB.Engine;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
unsafe internal struct DataBlock  // 12
{
    public byte DataFormat;       // 1
    public bool Extend;           // 1
    public byte Padding;          // 1  (0-7)
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public byte Reserved;         // 1

    public RowID NextBlockID; // 8

}
