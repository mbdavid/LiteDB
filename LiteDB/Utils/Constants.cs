using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LiteDB.Demo")]

namespace LiteDB
{
    /// <summary>
    /// Class with all constants used in LiteDB + Debbuger HELPER
    /// </summary>
    internal class Constants
    {
        /// <summary>
        /// The size of each page in disk - new v5 use 8192 as all major databases
        /// </summary>
        public const int PAGE_SIZE = 8192;

        /// <summary>
        /// Header page size (uses 3 page block)
        /// </summary>
        public const int PAGE_HEADER_SIZE = 96;

        /// <summary>
        /// Page are build over 256 page blocks of 32 bytes each
        /// </summary>
        public const int PAGE_BLOCK_SIZE = 32;

        /// <summary>
        /// Bytes available to store data removing page header size - 8128 bytes
        /// </summary>
        public const int PAGE_AVAILABLE_BYTES = PAGE_SIZE - PAGE_HEADER_SIZE;

        /// <summary>
        /// If a Data Page has less that free space (in blocks), it's considered full page for new items. Can be used only for update (DataPage)
        /// </summary>
        public const int DATA_RESERVED_BLOCKS = 32;

        /// <summary>
        /// If a Index Page has less that this free space (in blocks), it's considered full page for new items.
        /// </summary>
        public const int INDEX_RESERVED_BLOCKS = 8;

        /// <summary>
        /// Define max length to be used in a single collection name
        /// </summary>
        public const int COLLECTION_NAME_MAX_LENGTH = 60;

        /// <summary>
        /// Define (and reserve) bytes do be fixed in header page (are not considering collection name/id list)
        /// </summary>
        public const int HEADER_PAGE_FIXED_DATA_SIZE = 256;

        /// <summary>
        /// Represent maximum bytes that all collections names can be used in collection list page (must fit inside a single header page)
        /// Do not change - it's calculated 
        /// </summary>
        public const ushort MAX_COLLECTIONS_NAME_SIZE =
            PAGE_SIZE -
            PAGE_HEADER_SIZE -
            HEADER_PAGE_FIXED_DATA_SIZE;

        /// <summary>
        /// Define index name max length
        /// </summary>
        public static int INDEX_NAME_MAX_LENGTH = 32;

        /// <summary>
        /// Total indexes per collection - it's fixed because I will used fixed arrays allocations
        /// </summary>
        public const int INDEX_PER_COLLECTION = 32;

        /// <summary>
        /// Max level used on skip list (index).
        /// </summary>
        public const int MAX_LEVEL_LENGTH = 32;

        /// <summary>
        /// Max size of a index entry - usde for string, binary, array and documents
        /// </summary>
        public const int MAX_INDEX_KEY_LENGTH = 512;

        /// <summary>
        /// DocumentLoader max cache size
        /// </summary>
        public const int MAX_CACHE_DOCUMENT_LOADER_SIZE = 1000;

        /// <summary>
        /// Max cursor info history
        /// </summary>
        public const int MAX_CURSOR_HISTORY = 1000;

        /// <summary>
        /// Max pages in a transaction before persist on disk and clear transaction local pages
        /// </summary>
        public const int MAX_TRANSACTION_SIZE = 1000;

        /// <summary>
        /// Size, in PAGES, for each buffer array (used in MemoryStore) - Each byte array will be created with this size * PAGE_SIZE
        /// </summary>
        public const int MEMORY_SEGMENT_SIZE = 1000;

        /// <summary>
        /// Database header parameter: USERVERSION
        /// </summary>
        public const string DB_PARAM_USERVERSION = "USERVERSION";

        /// <summary>
        /// Stop VisualStudio if condition are true and we are running over #DEBUG - great for testing unexpected flow
        /// </summary>
        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void DEBUG(bool conditional, string message = null)
        {
            if(conditional)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    throw new SystemException(message);
                }
            }
        }
    }
}
