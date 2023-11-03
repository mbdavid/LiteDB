namespace LiteDB.Engine;

internal struct ExtendLocation : IIsEmpty
{
    public readonly int AllocationMapID; // 4
    public readonly int ExtendIndex;     // 4

    public bool IsEmpty => this.AllocationMapID == -1 && this.ExtendIndex == -1;

    public int ExtendID => this.IsEmpty ? -1 : (this.AllocationMapID * AM_EXTEND_COUNT) + this.ExtendIndex;

    public uint FirstPageID => GetFirstPageID(this.AllocationMapID, this.ExtendIndex);

    public ExtendLocation()
    {
        this.AllocationMapID = 0;
        this.ExtendIndex = 0;
    }

    public ExtendLocation(int allocationMapID, int extendIndex)
    {
        this.AllocationMapID = allocationMapID;
        this.ExtendIndex = extendIndex;

        ENSURE(extendIndex < AM_EXTEND_COUNT, new { self = this });
    }

    public ExtendLocation(int extendID)
    {
        this.AllocationMapID = extendID / AM_EXTEND_COUNT;
        this.ExtendIndex = extendID % AM_EXTEND_COUNT;
    }

    public override string ToString() => Dump.Object(this);

    public static uint GetFirstPageID(int allocationMapID, int extendIndex) => (uint)(allocationMapID * AM_PAGE_STEP + extendIndex * AM_EXTEND_SIZE + 1);

}
