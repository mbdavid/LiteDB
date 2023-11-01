namespace LiteDB.Engine;

[StructLayout(LayoutKind.Sequential, Size = PAGE_SIZE, Pack = 1)]
internal struct SortPage2
{
    public int ItemsCount; // 4
    public int Reserved;   // 4
}
