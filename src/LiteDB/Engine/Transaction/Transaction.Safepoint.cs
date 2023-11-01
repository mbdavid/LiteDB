namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal partial class Transaction : ITransaction
{
    /// <summary>
    /// Persist current pages changes and discard all local pages. Works as a Commit, but without
    /// marking last page as confirmed
    /// </summary>
    public async Task SafepointAsync()
    {
        using var _pc = PERF_COUNTER(130, nameof(SafepointAsync), nameof(Transaction));

        this.SafepointInternal();

        await _diskService.GetDiskWriter().FlushAsync();
    }

    private unsafe void SafepointInternal()
    {
        // get dirty pages only //TODO: can be re-used array?
        var dirtyPages = _localPages.Values
            .Where(x => ((PageMemory*)x)->IsDirty)
            .ToArray();

        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = (PageMemory*)dirtyPages[i];

            ENSURE(page->ShareCounter == NO_CACHE, "Page should not be on cache when saving");

            // update page header
            page->TransactionID = this.TransactionID;
            page->IsConfirmed = false;
        }

        // write pages on disk and flush data (updates PositionID and IsDirty = false)
        _logService.WriteLogPages(dirtyPages);

        // update local transaction wal index
        for (var i = 0; i < dirtyPages.Length; i++)
        {
            var page = (PageMemory*)dirtyPages[i];

            _walDirtyPages[page->PageID] = page->PositionID;
        }

        // add pages to cache or decrement sharecount
        foreach (var ptr in _localPages.Values)
        {
            var page = (PageMemory*)ptr;

            if (page->IsPageInCache)
            {
                // page already in cache (was not changed)
                _memoryCache.ReturnPageToCache(page);
            }
            else
            {
                // add page to cache (even if is on "non-commited" transaction)
                // only this transactions knows about this new positionIDs
                var added = _memoryCache.AddPageInCache(page);

                if (!added)
                {
                    _memoryFactory.DeallocatePage(page);
                }
            }
        }

        // clear page buffer references
        _localPages.Clear();
    }
}