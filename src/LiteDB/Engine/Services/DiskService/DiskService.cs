namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class DiskService : IDiskService
{
    // dependency injection
    private readonly IStreamFactory _streamFactory;
    private readonly IServicesFactory _factory;

    private readonly IDiskStream _writer;

    private readonly ConcurrentQueue<IDiskStream> _readers = new ();

    public IDiskStream GetDiskWriter() => _writer;

    public DiskService(
        IStreamFactory streamFactory,
        IServicesFactory factory)
    {
        _streamFactory = streamFactory;
        _factory = factory;

        _writer = factory.CreateDiskStream();
    }

    /// <summary>
    /// Open (or create) datafile.
    /// </summary>
    public async ValueTask<FileHeader> InitializeAsync()
    {
        // if file not exists, create empty database
        if (_streamFactory.Exists() == false)
        {
            // intialize new database class factory
            var newFile = _factory.CreateNewDatafile();

            // create first AM page and $master 
            return await newFile.CreateNewAsync(_writer);
        }
        else
        {
            // read header page buffer from start of disk
            return _writer.OpenFile(true);
        }
    }

    /// <summary>
    /// Rent a disk reader from pool. Must return after use
    /// </summary>
    public IDiskStream RentDiskReader()
    {
        if (_readers.TryDequeue(out var reader))
        {
            return reader;
        }

        // create new diskstream
        reader = _factory.CreateDiskStream();

        // and open to read-only (use saved header)
        reader.OpenFile(_factory.FileHeader);

        return reader;
    }

    /// <summary>
    /// Return a rented reader and add to pool
    /// </summary>
    public void ReturnDiskReader(IDiskStream reader)
    {
        _readers.Enqueue(reader);
    }

    public override string ToString()
    {
        return Dump.Object(new { _readers });
    }

    public void Dispose()
    {
        // dispose all open streams
        _writer.Dispose();

        foreach (var reader in _readers)
        {
            reader.Dispose();
        }

        // empty stream pool
        _readers.Clear();
    }
}
