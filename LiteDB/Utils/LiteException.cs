using System;
using System.Reflection;

namespace LiteDB
{
    /// <summary>
    /// The main exception for LiteDB
    /// </summary>
    public class LiteException : Exception
    {
        #region Errors code

        public const int NO_DATABASE = 100;
        public const int FILE_NOT_FOUND = 101;
        public const int FILE_CORRUPTED = 102;
        public const int INVALID_DATABASE = 103;
        public const int INVALID_DATABASE_VERSION = 104;
        public const int FILE_SIZE_EXCEEDED = 105;
        public const int COLLECTION_LIMIT_EXCEEDED = 106;
        public const int JOURNAL_FILE_FOUND = 107;
        public const int INDEX_DROP_IP = 108;
        public const int INDEX_LIMIT_EXCEEDED = 109;
        public const int INDEX_DUPLICATE_KEY = 110;
        public const int INDEX_KEY_TOO_LONG = 111;
        public const int INDEX_NOT_FOUND = 112;
        public const int INVALID_DBREF = 113;
        public const int LOCK_TIMEOUT = 120;
        public const int INVALID_COMMAND = 121;
        public const int ALREADY_EXISTS_COLLECTION_NAME = 122;
        public const int DATABASE_WRONG_PASSWORD = 123;
        public const int READ_ONLY_DATABASE = 125;
        public const int TRANSACTION_NOT_SUPPORTED = 126;

        public const int INVALID_FORMAT = 200;
        public const int DOCUMENT_MAX_DEPTH = 201;
        public const int INVALID_CTOR = 202;
        public const int UNEXPECTED_TOKEN = 203;
        public const int INVALID_DATA_TYPE = 204;
        public const int PROPERTY_NOT_MAPPED = 206;
        public const int INVALID_TYPED_NAME = 207;

        #endregion

        public int ErrorCode { get; private set; }

        public LiteException(string message)
            : base(message)
        {
        }

        internal LiteException(int code, string message, params object[] args)
            : base(string.Format(message, args))
        {
            this.ErrorCode = code;
        }

        internal LiteException (int code, Exception inner, string message, params object[] args)
        : base (string.Format (message, args), inner)
        {
            this.ErrorCode = code;
        }

        #region Database Errors

        internal static LiteException NoDatabase()
        {
            return new LiteException(NO_DATABASE, "There is no database.");
        }

        internal static LiteException FileNotFound(string fileId)
        {
            return new LiteException(FILE_NOT_FOUND, "File '{0}' not found.", fileId);
        }

        internal static LiteException FileCorrupted(string fileId)
        {
            return new LiteException(FILE_CORRUPTED, "File '{0}' has no content or is corrupted.", fileId);
        }

        internal static LiteException InvalidDatabase()
        {
            return new LiteException(INVALID_DATABASE, "Datafile is not a LiteDB database.");
        }

        internal static LiteException InvalidDatabaseVersion(int version)
        {
            return new LiteException(INVALID_DATABASE_VERSION, "Invalid database version: {0}", version);
        }

        internal static LiteException FileSizeExceeded(long limit)
        {
            return new LiteException(FILE_SIZE_EXCEEDED, "Database size exceeds limit of {0}.", StorageUnitHelper.FormatFileSize(limit));
        }

        internal static LiteException CollectionLimitExceeded(int limit)
        {
            return new LiteException(COLLECTION_LIMIT_EXCEEDED, "This database exceeded the maximum limit of collection names size: {0} bytes", limit);
        }

        internal static LiteException JournalFileFound(string journal)
        {
            return new LiteException(JOURNAL_FILE_FOUND, "Journal file found on '{0}'. Try to reopen the database.", journal);
        }

        internal static LiteException IndexDropId()
        {
            return new LiteException(INDEX_DROP_IP, "Primary key index '_id' can't be dropped.");
        }

        internal static LiteException IndexLimitExceeded(string collection)
        {
            return new LiteException(INDEX_LIMIT_EXCEEDED, "Collection '{0}' exceeded the maximum limit of indices: {1}", collection, CollectionIndex.INDEX_PER_COLLECTION);
        }

        internal static LiteException IndexDuplicateKey(string field, BsonValue key)
        {
            return new LiteException(INDEX_DUPLICATE_KEY, "Cannot insert duplicate key in unique index '{0}'. The duplicate value is '{1}'.", field, key);
        }

        internal static LiteException IndexKeyTooLong()
        {
            return new LiteException(INDEX_KEY_TOO_LONG, "Index key must be less than {0} bytes.", IndexService.MAX_INDEX_LENGTH);
        }

        internal static LiteException IndexNotFound(string collection, string field)
        {
            return new LiteException(INDEX_NOT_FOUND, "Index not found on '{0}.{1}'.", collection, field);
        }

        internal static LiteException LockTimeout(TimeSpan ts)
        {
            return new LiteException(LOCK_TIMEOUT, "Timeout. Database is locked for more than {0}.", ts.ToString());
        }

        internal static LiteException InvalidCommand(string command)
        {
            return new LiteException(INVALID_COMMAND, "Command '{0}' is not a valid shell command.", command);
        }

        internal static LiteException AlreadyExistsCollectionName(string newName)
        {
            return new LiteException(ALREADY_EXISTS_COLLECTION_NAME, "New collection name '{0}' already exists.", newName);
        }

        internal static LiteException DatabaseWrongPassword()
        {
            return new LiteException(DATABASE_WRONG_PASSWORD, "Invalid database password.");
        }

        internal static LiteException ReadOnlyDatabase()
        {
            return new LiteException(READ_ONLY_DATABASE, "This action are not supported because database was opened in read only mode.");
        }

        internal static LiteException InvalidDbRef(string path)
        {
            return new LiteException(INVALID_DBREF, "Invalid value for DbRef in path \"{0}\". Value must be document like {{ $ref: \"?\", $id: ? }}", path);
        }

        internal static LiteException TransactionNotSupported(string method)
        {
            return new LiteException(TRANSACTION_NOT_SUPPORTED, "Transactions are not supported here: " + method);
        }

        #endregion

        #region Document/Mapper Errors

        internal static LiteException InvalidFormat(string field, string format)
        {
            return new LiteException(INVALID_FORMAT, "Invalid format: {0}", field);
        }

        internal static LiteException DocumentMaxDepth(int depth, Type type)
        {
            return new LiteException(DOCUMENT_MAX_DEPTH, "Document has more than {0} nested documents in '{1}'. Check for circular references (use DbRef).", depth, type == null ? "-" : type.Name);
        }

        internal static LiteException InvalidCtor(Type type, Exception inner)
        {
            return new LiteException(INVALID_CTOR, inner, "Failed to create instance for type '{0}' from assembly '{1}'. Checks if the class has a public constructor with no parameters.", type.FullName, type.AssemblyQualifiedName);
        }

        internal static LiteException UnexpectedToken(string token)
        {
            return new LiteException(UNEXPECTED_TOKEN, "Unexpected JSON token: {0}", token);
        }

        internal static LiteException InvalidDataType(string field, BsonValue value)
        {
            return new LiteException(INVALID_DATA_TYPE, "Invalid BSON data type '{0}' on field '{1}'.", value.Type, field);
        }

        public const int PROPERTY_READ_WRITE = 204;

        internal static LiteException PropertyReadWrite(PropertyInfo prop)
        {
            return new LiteException(PROPERTY_READ_WRITE, "'{0}' property must have public getter and setter.", prop.Name);
        }

        internal static LiteException PropertyNotMapped(string name)
        {
            return new LiteException(PROPERTY_NOT_MAPPED, "Property '{0}' was not mapped into BsonDocument.", name);
        }

        internal static LiteException InvalidTypedName(string type)
        {
            return new LiteException(INVALID_TYPED_NAME, "Type '{0}' not found in current domain (_type format is 'Type.FullName, AssemblyName').", type);
        }

        #endregion
    }
}