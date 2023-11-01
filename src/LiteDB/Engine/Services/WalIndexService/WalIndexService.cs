namespace LiteDB.Engine;

/// <summary>
/// Do all WAL index services based on LOG file - has only single instance per engine
/// Do not work with PageBuffers, only with data position and version pointers
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class WalIndexService : IWalIndexService
{
    /// <summary>
    /// A indexed dictionary by PageID where each item are a sort list of read version and disk log position
    /// </summary>
    private readonly ConcurrentDictionary<uint, List<(int version, uint positionID)>> _index = new();

    /// <summary>
    /// Current read version
    /// </summary>
    private int _readVersion = 0;

    /// <summary>
    /// Minimal read version avaiable in WAL index (before checkpoint)
    /// </summary>
    public int MinReadVersion { get; private set; } = 1;

    public WalIndexService()
    {
    }

    /// <summary>
    /// Get next read version when transaction starts
    /// </summary>
    public int GetNextReadVersion()
    {
        return Interlocked.Increment(ref _readVersion);
    }

    /// <summary>
    /// Get a page positionID (in disk) for a page that are inside WAL. 
    /// If page is not in WAL index, return positionID on datafile
    /// </summary>
    public uint GetPagePositionID(uint pageID, int version, out int walVersion)
    {
        // initial value
        walVersion = 0;

        // if version is 0 or there is no page on log index, return current position on disk
        if (version == 0 || 
            _index.TryGetValue(pageID, out var listVersion) == false)
        {
            // in datafile - pageID = positionID
            return pageID;
        }

        // list are sorted by version number
        var idx = listVersion.Count;
        var positionID = pageID; // not found (get from data)

        // get all page versions in wal-index
        // and then filter only equals-or-less then selected version
        while (idx > 0)
        {
            idx--;

            var (ver, pos) = listVersion[idx];

            if (ver <= version)
            {
                walVersion = ver;
                positionID = pos;

                break;
            }
        }

        return positionID;
    }

    public void AddVersion(int version, IEnumerable<(uint pageID, uint positionID)> pagePositions)
    {
        foreach (var (pageID, positionID) in pagePositions)
        {
            if (_index.TryGetValue(pageID, out var listVersion))
            {
                // add version/position into pageID
                listVersion.Add(new(version, positionID));
            }
            else
            {
                listVersion = new()
                {
                    // add version/position into pageID
                    new(version, positionID)
                };

                // add listVersion with first item in index for this pageID
                _index.TryAdd(pageID, listVersion);
            }
        }
    }

    /// <summary>
    /// [ThreadSafe]
    /// </summary>
    public void Clear()
    {
        // reset minimal read version to next read version
        this.MinReadVersion = _readVersion + 1;

        _index.Clear();
    }

    public override string ToString()
    {
        return Dump.Object(this);
    }

    public void Dispose()
    {
        // reset fields
        _readVersion = 0;

        this.MinReadVersion = 1;

        _index.Clear();
    }
}