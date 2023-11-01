namespace LiteDB.Engine;

/// <summary>
/// Implement multiple readers/single writer for async task
/// </summary>
internal class AsyncReaderWriterLock : IDisposable
{
    private readonly SemaphoreSlim _readSemaphore = new (1, 1);
    private readonly SemaphoreSlim _writeSemaphore = new (1, 1);
    private readonly TimeSpan _timeout;
    private int _readerCount;

    public int ReaderCount => _readerCount;

    public AsyncReaderWriterLock(TimeSpan timeout)
    {
        //TODO: antes de usar o timeout, verificar que ao adicionar o metodo Wait retorna bool
        _timeout = timeout;
    }

    public async ValueTask AcquireWriterLock(CancellationToken token = default)
    {
        await _writeSemaphore.WaitAsync(_timeout, token).ConfigureAwait(false);

        try
        {
            await _readSemaphore.WaitAsync(_timeout, token).ConfigureAwait(false);
        }
        catch
        {
            _writeSemaphore.Release();
            throw;
        }
    }

    public void ReleaseWriterLock()
    {
        _readSemaphore.Release();
        _writeSemaphore.Release();
    }

    public async ValueTask AcquireReaderLock(CancellationToken token = default)
    {
        await _writeSemaphore.WaitAsync(_timeout, token).ConfigureAwait(false);

        if (Interlocked.Increment(ref _readerCount) == 1)
        {
            try
            {
                await _readSemaphore.WaitAsync(_timeout, token).ConfigureAwait(false);
            }
            catch
            {
                Interlocked.Decrement(ref _readerCount);
                _writeSemaphore.Release();

                throw;
            }
        }

        _writeSemaphore.Release();
    }

    public void ReleaseReaderLock()
    {
        if (Interlocked.Decrement(ref _readerCount) == 0)
        {
            _readSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _writeSemaphore.Dispose();
        _readSemaphore.Dispose();
    }
}

