namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal partial class Transaction : ITransaction
{
    /// <summary>
    /// </summary>
    public async ValueTask CommitAsync()
    {
        using var _pc = PERF_COUNTER(140, nameof(CommitAsync), nameof(Transaction));

        await this.CommitInternal();
    }

    private async ValueTask CommitInternal()
    {
        // get dirty pages only //TODO: can be re-used array?
        var dirtyPages = _localPages.Values
            .Where(x => x.IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = dirtyPages[i];

            ENSURE(page.ShareCounter == NO_CACHE, "Page should not be on cache when saving");

            // update page header
            page.TransactionID = this.TransactionID;
            page.IsConfirmed = i == (dirtyPages.Length - 1);
        }

        // write pages on disk and flush data (updates PositionID and IsDirty = false)
        await _logService.WriteLogPagesAsync(dirtyPages);

        // update wal index with this new version
        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = dirtyPages[i];

            _walDirtyPages[page.PageID] = page.PositionID;
        }

        // update wal index with new page set 
        _walIndexService.AddVersion(this.ReadVersion, _walDirtyPages.Select(x => (x.Key, x.Value)));

        // add pages to cache or decrement sharecount
        foreach (var page in _localPages.Values)
        {
            // page already in cache (was not changed)
            if (page.IsPageInCache)
            {
                _memoryCache.ReturnPageToCache(page);
            }
            else
            {
                // try add this page in cache
                var added = _memoryCache.AddPageInCache(page);

                if (!added)
                {
                    _memoryFactory.DeallocatePage(page);
                }
            }
        }

        // clear page buffer references
        _localPages.Clear();
        _walDirtyPages.Clear();
    }
}