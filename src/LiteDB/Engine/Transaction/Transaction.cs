namespace LiteDB.Engine;

/// <summary>
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal partial class Transaction : ITransaction
{
    // dependency injection
    private readonly IDiskService _diskService;
    private readonly ILogService _logService;
    private readonly IWalIndexService _walIndexService;
    private readonly IAllocationMapService _allocationMapService;
    private readonly IMemoryFactory _memoryFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ILockService _lockService;

    // count how many locks this transaction contains
    private int _lockCounter = 0;

    // rented reader stream
    private IDiskStream? _reader;

    // local page cache - contains only data/index pages about this collection
    private readonly Dictionary<uint, nint> _localPages = new();

    // when safepoint occurs, save reference for changed pages on log (PageID, PositionID)
    private readonly Dictionary<uint, uint> _walDirtyPages = new();

    // original extend values from all requested writable pages (ExtendID, ExtendValue)
    private readonly Dictionary<int, uint> _initialExtendValues = new();

    // all writable collections ID (must be lock on init)
    private readonly byte[] _writeCollections;

    // for each writeCollection, a cursor for current extend disk position (for data/index per collection)
    private readonly ExtendLocation[] _currentIndexExtend;
    private readonly ExtendLocation[] _currentDataExtend;

    /// <summary>
    /// Read wal version
    /// </summary>
    public int ReadVersion { get; private set; }

    /// <summary>
    /// Incremental transaction ID
    /// </summary>
    public int TransactionID { get; }

    /// <summary>
    /// Get how many pages, in memory, this transaction are using
    /// </summary>
    public int PagesUsed => _localPages.Count;

    public Transaction(
        IDiskService diskService,
        ILogService logService,
        IMemoryFactory memoryFactory,
        IMemoryCache memoryCache,
        IWalIndexService walIndexService,
        IAllocationMapService allocationMapService,
        ILockService lockService,
        int transactionID, byte[] writeCollections, int readVersion)
    {
        _diskService = diskService;
        _logService = logService;
        _memoryFactory = memoryFactory;
        _memoryCache = memoryCache;
        _walIndexService = walIndexService;
        _allocationMapService = allocationMapService;
        _lockService = lockService;

        this.TransactionID = transactionID;
        this.ReadVersion = readVersion; // -1 means not initialized

        _writeCollections = writeCollections;
        _currentIndexExtend = new ExtendLocation[writeCollections.Length];
        _currentDataExtend = new ExtendLocation[writeCollections.Length];
    }

    /// <summary>
    /// Initialize transaction enter in database read lock
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        // enter transaction lock
        await _lockService.EnterTransactionAsync();

        for(var i = 0; i < _writeCollections.Length; i++)
        {
            // enter in all
            await _lockService.EnterCollectionWriteLockAsync(_writeCollections[i]);

            // increment lockCounter to dispose control
            _lockCounter++;
        }

        // if readVersion is -1 must be initialized with next read version from wal
        if (this.ReadVersion == -1)
        {
            // initialize read version from wal
            this.ReadVersion = _walIndexService.GetNextReadVersion();
        }

        ENSURE(this.ReadVersion >= _walIndexService.MinReadVersion, $"Read version do not exists in wal index: {this.ReadVersion} >= {_walIndexService.MinReadVersion}", new { self = this });
    }

    public override string ToString()
    {
        return Dump.Object(new { TransactionID, ReadVersion, _localPages, _writeCollections, _initialExtendValues, _currentIndexExtend, _currentDataExtend, _lockCounter });
    }

    public void Dispose()
    {
        // return reader if used
        if (_reader is not null)
        {
            _diskService.ReturnDiskReader(_reader);
        }

        // Transaction = 0 means is created for first $master read. There are an ExclusiveLock befre
        if (this.TransactionID == 0) return;

        while (_lockCounter > 0)
        {
            _lockService.ExitCollectionWriteLock(_writeCollections[_lockCounter - 1]);
            _lockCounter--;
        }

        // exit lock transaction
        _lockService.ExitTransaction();

        ENSURE(_localPages.Count == 0, $"Missing dispose pages in transaction", new { _localPages });
        ENSURE(_lockCounter == 0, $"Missing release lock in transaction", new { _localPages, _lockCounter });
    }
}