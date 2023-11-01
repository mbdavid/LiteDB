namespace LiteDB.Engine;

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
unsafe internal partial struct PageMemory             // 8192 (64 bytes header - 8128 content)
{
    [FieldOffset(00)] public uint PositionID;         // 4
    [FieldOffset(04)] public uint PageID;             // 4 *

    [FieldOffset(08)] public PageType PageType;       // 1
    [FieldOffset(09)] public byte ColID;              // 1
    [FieldOffset(10)] public bool IsDirty;            // 1    - memory only control (but stores on disk default value)
    [FieldOffset(11)] public bool IsConfirmed;        // 1

    [FieldOffset(12)] public int ShareCounter;        // 4 *  - memory only control (but stores on disk default value)
    [FieldOffset(16)] public int UniqueID;            // 4    - memory only control (but stores on disk default value)
    [FieldOffset(20)] public int TransactionID;       // 4 *
    [FieldOffset(24)] public uint RecoveryPositionID; // 4

    [FieldOffset(28)] public ushort ItemsCount;       // 2
    [FieldOffset(30)] public ushort UsedBytes;        // 2 *
    [FieldOffset(32)] public ushort FragmentedBytes;  // 2
    [FieldOffset(34)] public ushort NextFreeLocation; // 2
    [FieldOffset(36)] public short HighestIndex;      // 2  use -1 to unset

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    [FieldOffset(38)] public ushort Reserved1;        // 2 *
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    [FieldOffset(40)] public ulong Reserved2;         // 8 *
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    [FieldOffset(48)] public ulong Reserved3;         // 8 *
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    [FieldOffset(56)] public uint Reserved4;          // 4

    [FieldOffset(60)] public int Crc32;               // 4 *

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
    public ExtendPageValue ExtendPageValue => PageMemory.GetExtendPageValue(this.PageType, this.FreeBytes);

    public bool IsPageInLogFile => this.PositionID != this.PageID;
    public bool IsPageInCache => this.ShareCounter != NO_CACHE;

    public PageMemory()
    {
    }

    public static void Initialize(PageMemory* page, int uniqueID)
    {
        page->PositionID = uint.MaxValue;
        page->PageID = uint.MaxValue;

        page->PageType = PageType.Empty;
        page->ColID = 0;
        page->IsDirty = false;
        page->IsConfirmed = false;

        page->Reserved1 = 0;
        page->Reserved2 = 0;
        page->Reserved3 = 0;
        page->Reserved4 = 0;

        page->ShareCounter = NO_CACHE;
        page->UniqueID = uniqueID;
        page->TransactionID = 0;
        page->RecoveryPositionID = uint.MaxValue;

        page->ItemsCount = 0;
        page->UsedBytes = 0;
        page->FragmentedBytes = 0;
        page->NextFreeLocation = PAGE_HEADER_SIZE; // first location
        page->HighestIndex = -1;

        page->Crc32 = 0;

        // clear full content area
        MarshalEx.FillZero((byte*)(nint)page + PAGE_HEADER_SIZE, PAGE_CONTENT_SIZE);
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
        return Dump.Object(new { PositionID, PageID, PageType, ColID, IsDirty, IsConfirmed, ShareCounter, TransactionID, ItemsCount, FreeBytes });
    }
}
