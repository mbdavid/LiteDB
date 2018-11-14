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
        /// Header page size (use first page block - 32 bytes)
        /// </summary>
        public const int PAGE_HEADER_SIZE = 32;

        /// <summary>
        /// Page are build over 256 page blocks of 32 bytes each
        /// </summary>
        public const int PAGE_BLOCK_SIZE = 32;

        /// <summary>
        /// Bytes available to store data removing page header size - 8128 bytes
        /// </summary>
        public const int PAGE_AVAILABLE_BLOCKS = (PAGE_SIZE - PAGE_HEADER_SIZE) / PAGE_SIZE;

        /// <summary>
        /// Bytes used in encryption salt
        /// </summary>
        public const int ENCRYPTION_SALT_SIZE = 16;

        /// <summary>
        /// Position, in file (header page) that contains SALT encryption (position 8176)
        /// </summary>
        public const int P_HEADER_SALT = PAGE_SIZE - ENCRYPTION_SALT_SIZE;

        /// <summary>
        /// Define index name max length
        /// </summary>
        public static int INDEX_NAME_MAX_LENGTH = 32;

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
