namespace LiteDB;

/// <summary>
/// Class with all constants used in LiteDB + Debbuger HELPER
/// </summary>
internal class Constants
{
    /// <summary>
    /// Initial file data descriptor size (before start database - use offset in Stream)
    /// </summary>
    public const int FILE_HEADER_SIZE = 96;

    /// <summary>
    /// The size of each page in disk - use 8192 as all major databases
    /// </summary>
    public const int PAGE_SIZE = 8192;

    /// <summary>
    /// Header page size
    /// </summary>
    public const int PAGE_HEADER_SIZE = 64;

    /// <summary>
    /// Get page content area size (8128)
    /// </summary>
    public const int PAGE_CONTENT_SIZE = PAGE_SIZE - PAGE_HEADER_SIZE;

    /// <summary>
    /// Get a full empty array with PAGE_SIZE (do not change any value - shared instance)
    /// </summary>
    public static readonly byte[] PAGE_EMPTY = new byte[PAGE_SIZE];

    /// <summary>
    /// Get a full empty array with PAGE_SIZE (do not change any value - shared instance)
    /// </summary>
    public static readonly Memory<byte> PAGE_EMPTY_BUFFER = new byte[PAGE_SIZE];

    /// <summary>
    /// Bytes used in encryption salt
    /// </summary>
    public const int ENCRYPTION_SALT_SIZE = 16;

    /// <summary>
    /// File header info (27 bytes length)
    /// </summary>
    public const string FILE_HEADER_INFO = "** This is a LiteDB file **";

    /// <summary>
    /// Current file version
    /// </summary>
    public const byte FILE_VERSION = 9;

    /// <summary>
    /// Represent pageID of first AllocationMapPage (#0)
    /// </summary>
    public const uint AM_FIRST_PAGE_ID = 0;

    /// <summary>
    /// Represent how many pages each extend will allocate in AllocationMapPage
    /// </summary>
    public const int AM_EXTEND_SIZE = 8;

    /// <summary>
    /// Bytes used in each extend (8 pages)
    /// 1 byte for colID + 3 bytes for 8 pages bit wise for Empty|Data|Index|Reserved[4]|Full
    /// </summary>
    public const int AM_BYTES_PER_EXTEND = 4;

    /// <summary>
    /// Get how many extends a single AllocationMap page support (2.032 extends)
    /// </summary>
    public const int AM_EXTEND_COUNT = PAGE_CONTENT_SIZE / AM_BYTES_PER_EXTEND;

    /// <summary>
    /// Get how many pages (data/index/empty) a single allocation map page support (16.256 pages)
    /// </summary>
    public const int AM_MAP_PAGES_COUNT = AM_EXTEND_COUNT * AM_EXTEND_SIZE;

    /// <summary>
    /// Indicate how many allocation map pages will jump to another map page (starts in 0)
    /// </summary>
    public const int AM_PAGE_STEP = AM_MAP_PAGES_COUNT + 1;

    /// <summary>
    /// Represent an array of how distribuited pages are inside AllocationMap using 3 bits
    /// [000] - 0 - Empty Page (can be used for both data/index)
    /// [001] - 1 - Data Page with free space (more than 30%)
    /// [010] - 2 - Index Page with free space (more than 300 bytes)
    /// [111] - 7 - Page Full (for both data/index pages)
    /// </summary>
    public const int AM_DATA_PAGE_FREE_SPACE = (int)(PAGE_CONTENT_SIZE * 0.3); // 2248;
    public const int AM_INDEX_PAGE_FREE_SPACE = 300;

    /// <summary>
    /// Get first DataPage from $master
    /// </summary>
    public const int MASTER_PAGE_ID = 1;

    /// <summary>
    /// Get colID for $master document
    /// </summary>
    public const byte MASTER_COL_ID = 255;

    /// <summary>
    /// Get colID for $master document
    /// </summary>
    public static RowID MASTER_ROW_ID = new(MASTER_PAGE_ID, 0);

    /// <summary>
    /// Get max colID for collections to be used by user (1..LIMIT)
    /// </summary>
    public const int MASTER_COL_LIMIT = 250;

    /// <summary>
    /// Define when ShareCounter (on PageBuffer) are in cache or not
    /// </summary>
    public const int NO_CACHE = -1;

    /// <summary>
    /// Initial seed for Random
    /// </summary>
    public const int RANDOMIZER_SEED = 42901;

    /// <summary>
    /// Define index name max length
    /// </summary>
    public const int MAX_INDEX_KEY_SIZE = 256;

    /// <summary>
    /// Define index name max length
    /// </summary>
    public const int INDEX_MAX_NAME_LENGTH = 32;

    /// <summary>
    /// Max level used on skip list (index).
    /// </summary>
    public const int INDEX_MAX_LEVELS = 32;

    /// <summary>
    /// Max size of a index entry - usde for string, binary, array and documents. Need fit in 1 byte length
    /// </summary>
    public const int INDEX_MAX_KEY_LENGTH = 1023;

    /// <summary>
    /// Get default checkpoint size (in pages). This value is used inside pragma
    /// </summary>
    public const int CHECKPOINT_SIZE = 1_000;

    /// <summary>
    /// A simple memory managment - store in disk when a transaction get this counter pages
    /// </summary>
    public const int SAFEPOINT_SIZE = 1_000;

    /// <summary>
    /// Max number to keep pages in cache. After this, a CleanUp() should be execute
    /// </summary>
    public const int CACHE_LIMIT = 25_000;

    /// <summary>
    /// Define how many pages each sort container should have
    /// </summary>
    public const int CONTAINER_SORT_SIZE_IN_PAGES = 100;

    /// <summary>
    /// Initial unique ID for BufferFactory allocate new pages
    /// </summary>
    public const int BUFFER_UNIQUE_ID = 100;

    /// <summary>
    /// Special field name created when query constains GroupBy. Key field will be created with this name when GroupBy expression isn't a PathExpression
    /// </summary>
    public const string GROUP_BY_KEY_NAME = "key";


    public static long TICK_FREQUENCY = (TimeSpan.TicksPerSecond / Stopwatch.Frequency);

}
