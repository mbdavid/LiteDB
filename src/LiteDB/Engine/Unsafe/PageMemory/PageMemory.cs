namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
unsafe internal partial struct PageMemory             // 8192 (64 bytes header - 8128 content)
{
    [FieldOffset(00)] public uint PositionID;         // 4
    [FieldOffset(04)] public uint PageID;             // 4 * (8)

    [FieldOffset(08)] public PageType PageType;       // 1
    [FieldOffset(09)] public byte ColID;              // 1
    [FieldOffset(10)] public bool IsConfirmed;        // 1
    [FieldOffset(11)] public bool IsDirty;            // 1
    [FieldOffset(12)] public int TransactionID;       // 4 * (16)

    [FieldOffset(16)] public uint RecoveryPositionID; // 4
    [FieldOffset(20)] public ushort ItemsCount;       // 2
    [FieldOffset(22)] public ushort UsedBytes;        // 2 * (24)

    [FieldOffset(24)] public ushort FragmentedBytes;  // 2
    [FieldOffset(26)] public ushort NextFreeLocation; // 2
    [FieldOffset(28)] public short HighestIndex;      // 2  use -1 to unset
    [FieldOffset(30)] public ushort Reserved1;        // 2 * (32)

    [FieldOffset(32)] public ulong Reserved2;         // 8 * (40)
    [FieldOffset(40)] public ulong Reserved3;         // 8 * (48)
    [FieldOffset(48)] public ulong Reserved4;         // 8 * (56)

    [FieldOffset(56)] public uint Reserved5;          // 4
    [FieldOffset(60)] public int Crc32;               // 4 * (64)

    [FieldOffset(PAGE_HEADER_SIZE)] public fixed byte Buffer[PAGE_CONTENT_SIZE];  // 8128
    [FieldOffset(PAGE_HEADER_SIZE)] public fixed uint Extends[AM_EXTEND_COUNT];   // 8128


    /// <summary>
    /// Get how many free bytes (including fragmented bytes) are in this page (content space) - Will return 0 bytes if page are full (or with max 255 items)
    /// </summary>
    public int FreeBytes => PAGE_CONTENT_SIZE - this.UsedBytes - this.FooterSize;

    /// <summary>
    /// Get how many bytes are used in footer page at this moment. Should align in  8 bytes
    /// ((HighestIndex + 1) * segment (4) + padding 8
    /// </summary>
    public int FooterSize => this.HighestIndex == -1 ? 0 :  // no items in page
        ((this.HighestIndex + 1) % 2 == 1 ? (this.HighestIndex + 2) : (this.HighestIndex + 1)) // align in 8 bytes
        * sizeof(PageSegment); // 4 bytes PER item (2 to position + 2 to length) - need consider HighestIndex used

    /// <summary>
    /// Get current extend page value based on PageType and FreeSpace
    /// </summary>
    public unsafe ExtendPageValue ExtendPageValue => PageMemory.GetExtendPageValue(this.PageType, this.FreeBytes);

    public PageMemory()
    {
    }

    public static void Initialize(PageMemory* page)
    {
        page->PositionID = uint.MaxValue;
        page->PageID = uint.MaxValue;

        page->PageType = PageType.Empty;
        page->ColID = 0;
        page->IsConfirmed = false;
        page->IsDirty = false;

        page->TransactionID = 0;
        page->RecoveryPositionID = uint.MaxValue;

        page->ItemsCount = 0;
        page->UsedBytes = 0;
        page->FragmentedBytes = 0;
        page->NextFreeLocation = PAGE_HEADER_SIZE; // first location
        page->HighestIndex = -1;

        page->Reserved1 = 0;
        page->Reserved2 = 0;
        page->Reserved3 = 0;
        page->Reserved4 = 0;
        page->Reserved5 = 0;

        page->Crc32 = 0;

        // clear full content area
        MarshalEx.FillZero((byte*)((nint)page) + PAGE_HEADER_SIZE, PAGE_CONTENT_SIZE);
    }

    public string DumpPage()
    {
        fixed(PageMemory* page = &this)
        {
            return PageDump.Render(page);
        }
    }

    public override string ToString()
    {
        return Dump.Object(new { PositionID, PageID, PageType, ColID, IsConfirmed, TransactionID, ItemsCount, FreeBytes });
    }
}
