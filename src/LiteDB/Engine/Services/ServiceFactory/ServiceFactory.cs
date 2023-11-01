namespace LiteDB;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal partial class ServicesFactory : IServicesFactory
{
    #region Public Properties

    public IEngineSettings Settings { get; }
    public EngineState State { get; set; } = EngineState.Close;
    public FileHeader FileHeader { get; set; }
    public Exception? Exception { get; set; }

    public IBsonReader BsonReader { get; }
    public IBsonWriter BsonWriter { get; }

    public IMemoryFactory MemoryFactory { get; }
    public IStreamFactory StreamFactory { get; }
    public IStreamFactory SortStreamFactory { get; }
    public IDocumentStoreFactory StoreFactory { get; }


    public IMemoryCache MemoryCache { get; }

    public ILogService LogService { get; }
    public IWalIndexService WalIndexService { get; }

    public ISortService SortService { get; }
    public IQueryService QueryService { get; }

    public ILockService LockService { get; }
    public IDiskService DiskService { get; }
    public IRecoveryService RecoveryService { get; }
    public IAllocationMapService AllocationMapService { get; }
    public IMasterMapper MasterMapper { get; }
    public IMasterService MasterService { get; }
    public IMonitorService MonitorService { get; }
    public IAutoIdService AutoIdService { get; }

    #endregion

    public ServicesFactory(IEngineSettings settings)
    {
        // get settings instance
        this.Settings = settings;

        // intial state
        this.FileHeader = new ();
        this.State = EngineState.Close;
        this.Exception = null;

        // no dependencies
        this.BsonReader = new BsonReader();
        this.BsonWriter = new BsonWriter();
        this.WalIndexService = new WalIndexService();
        this.MemoryFactory = new MemoryFactory();
        this.StoreFactory = new DocumentStoreFactory();
        this.MasterMapper = new MasterMapper();
        this.AutoIdService = new AutoIdService();

        // settings dependency only
        this.LockService = new LockService(settings.Timeout);
        this.StreamFactory = settings.Filename is not null ?
            new FileStreamFactory(settings.Filename, settings.ReadOnly) :
            new FileStreamFactory("implementar MemoryStream", false);

        this.SortStreamFactory = settings.Filename is not null ?
            new FileSortStreamFactory(settings.Filename) :
            new FileStreamFactory("implementar MemoryStream", false);

        // other services dependencies
        this.MemoryCache = new MemoryCache(this.MemoryFactory);
        this.DiskService = new DiskService(this.StreamFactory, this);
        this.LogService = new LogService(this.DiskService, this.MemoryCache, this.MemoryFactory, this.WalIndexService, this);
        this.AllocationMapService = new AllocationMapService(this.DiskService, this.MemoryFactory);
        this.MasterService = new MasterService(this);
        this.MonitorService = new MonitorService(this);
        this.RecoveryService = new RecoveryService(this.MemoryFactory, this.DiskService);
        this.SortService = new SortService(this.SortStreamFactory, this);
        this.QueryService = new QueryService(this.WalIndexService, this);
    }

    #region Transient instances ("Create" prefix)

    public IDiskStream CreateDiskStream()
        => new DiskStream(this.Settings, this.StreamFactory);

    public IQueryOptimization CreateQueryOptimization()
        => new QueryOptimization(this);

    public IDataReader CreateDataReader(Cursor cursor, int fetchSize, IServicesFactory factory)
        => new BsonDataReader(cursor, fetchSize, factory);

    public NewDatafile CreateNewDatafile() => new NewDatafile(
        this.MemoryFactory,
        this.MasterMapper,
        this.BsonWriter,
        this.Settings);

    public ITransaction CreateTransaction(int transactionID, byte[] writeCollections, int readVersion) => new Transaction(
        this.DiskService,
        this.LogService,
        this.MemoryFactory,
        this.MemoryCache,
        this.WalIndexService,
        this.AllocationMapService,
        this.LockService,
        transactionID, writeCollections, readVersion);

    public IDataService CreateDataService(ITransaction transaction) => new DataService(
        this.BsonReader, 
        this.BsonWriter, 
        transaction);

    public IIndexService CreateIndexService(ITransaction transaction) => new IndexService(
        this.FileHeader.Collation,
        transaction);

    public PipelineBuilder CreatePipelineBuilder(IDocumentStore store, BsonDocument queryParameters) => new PipelineBuilder(
            this.MasterService,
            this.SortService,
            this.FileHeader.Collation,
            store,
            queryParameters);

    public ISortOperation CreateSortOperation(OrderBy orderBy) => new SortOperation(
        this.SortService,
        this.FileHeader.Collation,
        this,
        orderBy);

    public ISortContainer CreateSortContainer(int containerID, int order, Stream stream) => new SortContainer(
        this.FileHeader.Collation,
        containerID,
        order,
        stream);

    #endregion

    public void Dispose()
    {
        // dispose all instances services to keep all clean (disk/memory)

        // variables/lists only
        this.WalIndexService.Dispose();
        this.LockService.Dispose();
        this.RecoveryService.Dispose();

        // pageBuffer dependencies
        this.MemoryCache.Dispose();
        this.LogService.Dispose();
        this.AllocationMapService.Dispose();
        this.MasterService.Dispose();
        this.MonitorService.Dispose();
        this.DiskService.Dispose();

        // dispose buffer pages
        this.MemoryFactory.Dispose();

        this.State = EngineState.Close;

        // keeps "Exception" value (will be clean in next open)
        // keeps "FileHeader"
        // keeps "State"


    }
}