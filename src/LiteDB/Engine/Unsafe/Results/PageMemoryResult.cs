namespace LiteDB.Engine;

internal class PageMemoryResult : IIsEmpty, IDisposable
{
    public readonly int UniqueID;

    public int ShareCounter;

    public readonly byte[] Buffer;

    public readonly nint Ptr;

    public unsafe PageMemory* Page;

    private readonly GCHandle? _handle;

    public bool IsEmpty => this.UniqueID == 0;

    public static readonly PageMemoryResult Empty = new();

    public unsafe PageMemoryResult(int uniqueID)
    {
        this.UniqueID = uniqueID;
        this.ShareCounter = NO_CACHE;
        this.Buffer = new byte[PAGE_SIZE];

        _handle = GCHandle.Alloc(this.Buffer, GCHandleType.Pinned);

        this.Ptr = _handle.Value.AddrOfPinnedObject();

        this.Page = (PageMemory*)this.Ptr;

        this.Initialize();
    }

    private PageMemoryResult()
    {
        this.UniqueID = 0;
        this.ShareCounter = 0;
        this.Buffer = [];
        this.Ptr = IntPtr.Zero;

        _handle = null;
    }

    public unsafe void Initialize()
    {
        this.ShareCounter = NO_CACHE;

        PageMemory.Initialize(this.Page);
    }

    #region Shortcuts for safe access

    /// <summary>
    /// Shortcut for get/set Page->PageID
    /// </summary>
    public unsafe uint PageID { get => this.Page->PageID; set => this.Page->PageID= value; }

    /// <summary>
    /// Shortcut for get/set Page->PositionID
    /// </summary>
    public unsafe uint PositionID { get => this.Page->PositionID; set => this.Page->PositionID = value; }

    /// <summary>
    /// Shortcut for get/set Page->PageType
    /// </summary>
    public unsafe PageType PageType { get => this.Page->PageType; set => this.Page->PageType = value; }

    /// <summary>
    /// Shortcut for get/set Page->ColID
    /// </summary>
    public unsafe byte ColID { get => this.Page->ColID; set => this.Page->ColID = value; }

    /// <summary>
    /// Shortcut for get/set Page->IsDirty
    /// </summary>
    public unsafe bool IsDirty { get => this.Page->IsDirty; set => this.Page->IsDirty = value; }

    /// <summary>
    /// Shortcut for get/set Page->TransactionID
    /// </summary>
    public unsafe int TransactionID { get => this.Page->TransactionID; set => this.Page->TransactionID = value; }

    /// <summary>
    /// Shortcut for get/set Page->IsConfirmed
    /// </summary>
    public unsafe bool IsConfirmed { get => this.Page->IsConfirmed; set => this.Page->IsConfirmed = value; }

    /// <summary>
    /// Shortcut for get/set Page->RecoveryPositionID
    /// </summary>
    public unsafe uint RecoveryPositionID { get => this.Page->RecoveryPositionID; set => this.Page->RecoveryPositionID = value; }

    /// <summary>
    /// Shortcut for get Page->FreeBytes
    /// </summary>
    public unsafe int FreeBytes => this.Page->FreeBytes;

    ///// <summary>
    ///// Get current extend page value based on PageType and FreeSpace
    ///// </summary>
    //public unsafe ExtendPageValue ExtendPageValue => PageMemory.GetExtendPageValue(this.Page->PageType, this.Page->FreeBytes);

    #endregion

    public bool IsPageInLogFile => this.PositionID != this.PageID;
    public bool IsPageInCache => this.ShareCounter != NO_CACHE;

    public void Dispose()
    {
        _handle?.Free();
    }
}
