namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal partial class Transaction : ITransaction
{
    /// <summary>
    /// This operation will write directly into WAL with empty pages.
    /// Used only in DropCollection.
    /// </summary>
    public unsafe void DeletePages(IReadOnlyList<uint> pages)
    {
        using var _pc = PERF_COUNTER(97, nameof(DeletePages), nameof(Transaction));

        ENSURE(_localPages.Count == 0, "no local pages required");

        // remove from cache 
        foreach(var pageID in pages)
        {
            var positionID = _walIndexService.GetPagePositionID(pageID, this.ReadVersion, out _);

            // if page are in local wal, remove from cache
            var removed = _memoryCache.TryRemove(positionID, out var page);

            if (removed)
            {
                // this page can be released
                _memoryFactory.DeallocatePage(page);
            }
        }

        // write empty log pages and load _walDrityPages to be added in commit
        _logService.WriteEmptyLogPages(pages, this.TransactionID, _walDirtyPages);
    }
}