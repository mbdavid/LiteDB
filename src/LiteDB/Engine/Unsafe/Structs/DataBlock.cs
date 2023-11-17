namespace LiteDB.Engine;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
internal struct DataBlock         // 12
{
    public byte DataFormat;       // 1
    public bool Extend;           // 1
    public byte Padding;          // 1  (0-7)
    public byte Reserved;         // 1

    public RowID NextBlockID;     // 8
}
