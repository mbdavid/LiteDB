namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal partial class Transaction : ITransaction
{
    public unsafe void Abort()
    {
        using var _pc = PERF_COUNTER(150, nameof(Abort), nameof(Transaction));

        // add pages to cache or decrement sharecount
        foreach (var ptr in _localPages.Values)
        {
            var page = (PageMemory*)ptr;

            if (page->IsDirty)
            {
                _memoryFactory.DeallocatePage(page);
            }
            else
            {
                // test if page is came from the cache
                if (page->IsPageInCache)
                {
                    // return page to cache
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
        }

        // clear page buffer references
        _localPages.Clear();
        _walDirtyPages.Clear();

        // restore initial values in allocation map to return original state before any change
        if (_initialExtendValues.Count > 0)
        {
            _allocationMapService.RestoreExtendValues(_initialExtendValues);
        }

        _initialExtendValues.Clear();
    }
}