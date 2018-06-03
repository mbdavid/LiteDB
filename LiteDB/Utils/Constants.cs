using System;
using System.Diagnostics;

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
        /// This size is used bytes in header pages 33 bytes (+31 reserved to future use) = 64 bytes
        /// </summary>
        public const int PAGE_HEADER_SIZE = 64;

        /// <summary>
        /// Bytes available to store data removing page header size - 8128 bytes
        /// </summary>
        public const int PAGE_AVAILABLE_BYTES = PAGE_SIZE - PAGE_HEADER_SIZE;

        /// <summary>
        /// If a Data Page has less that free space, it's considered full page for new items. Can be used only for update (DataPage)
        /// </summary>
        public const int DATA_RESERVED_BYTES = 1000;

        /// <summary>
        /// If a Index Page has less that this free space, it's considered full page for new items.
        /// </summary>
        public const int INDEX_RESERVED_BYTES = 500;

        /// <summary>
        /// Represent maximum bytes that all parameters must store in header page.
        /// It's includes all key+values (Parameters are BsonDocument)
        /// </summary>
        public const ushort MAX_PARAMETERS_SIZE = 1024;

        /// <summary>
        /// Define max length to be used in a single collection name
        /// </summary>
        public const int COLLECTION_NAME_MAX_LENGTH = 60;

        /// <summary>
        /// Represent maximum bytes that all collections names can be used in collection list page (must fit inside a single header page)
        /// Do not change - it's calc
        /// </summary>
        public const ushort MAX_COLLECTIONS_NAME_SIZE = 
            PAGE_SIZE -
            PAGE_HEADER_SIZE -
            64 - // used in page
            192 - // reserved (total: 256)
            MAX_PARAMETERS_SIZE;

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
        /// Max transactions (inactive) must be keeped in queue (can be visible during $transactions virtual collection)
        /// </summary>
        public const int MAX_TRANSACTION_BUFFER = 100;

        /// <summary>
        /// Define how many pages must be in-memory during a transaction. After this number, flush all dirty pages into disk
        /// </summary>
        public const int MAX_PAGES_TRANSACTION = 5;

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
