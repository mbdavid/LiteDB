namespace LiteDB.Engine;

/// <summary>
/// Lock service are collection-based locks. Lock will support any threads reading at same time. Writing operations will be locked
/// based on collection. Eventualy, write operation can change header page that has an exclusive locker for.
/// [ThreadSafe]
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class LockService : ILockService
{
    private readonly TimeSpan _timeout;

    private AsyncReaderWriterLock? _database;
    // preciso de disk_writer 

    private readonly AsyncReaderWriterLock[] _collections;

    public LockService(TimeSpan timeout)
    {
        _timeout = timeout;

        // create all 0-255 possible collections (initialize only used)
        _collections = new AsyncReaderWriterLock[byte.MaxValue + 1];
    }

    /// <summary>
    /// Return how many transactions are opened
    /// </summary>
    public int TransactionsCount => _database?.ReaderCount ?? 0;

    /// <summary>
    /// All non-exclusive database operations must call this EnterTranscation() just before working. 
    /// This will be used to garantee exclusive write-only (non-reader) during exclusive operations (like checkpoint)
    /// </summary>
    public async ValueTask EnterTransactionAsync()
    {
        _database ??= new AsyncReaderWriterLock(_timeout);

        await _database.AcquireReaderLock();
    }

    /// <summary>
    /// Exit transaction read lock
    /// </summary>
    public void ExitTransaction()
    {
        _database!.ReleaseReaderLock();
    }

    /// <summary>
    /// Enter all database in exclusive lock. Wait for all transactions finish. In exclusive mode no one can enter in new transaction (for read/write)
    /// If current thread already in exclusive mode, returns false
    /// </summary>
    public async ValueTask EnterExclusiveAsync()
    {
        _database ??= new AsyncReaderWriterLock(_timeout);

        await _database.AcquireWriterLock();
    }

    /// <summary>
    /// Exit exclusive lock
    /// </summary>
    public void ExitExclusive()
    {
        _database!.ReleaseWriterLock();
    }

    /// <summary>
    /// Enter collection write lock mode (only 1 collection per time can have this lock)
    /// </summary>
    public async ValueTask EnterCollectionWriteLockAsync(byte colID)
    {
        var locker = _collections[colID] ??= new AsyncReaderWriterLock(_timeout);

        await locker.AcquireWriterLock();
    }

    /// <summary>
    /// Exit collection in reserved lock
    /// </summary>
    public void ExitCollectionWriteLock(byte colID)
    {
        _collections[colID].ReleaseWriterLock();
    }

    public override string ToString()
    {
        return Dump.Object(new { TransactionsCount });
    }

    public void Dispose()
    {
        try
        {
            _database?.Dispose();
            _database = null;

            for (var i = 0; i < _collections.Length; i++)
            {
                _collections[i]?.Dispose();
                _collections[i] = default!;
            }
        }
        catch (SynchronizationLockException)
        {
        }
    }
}