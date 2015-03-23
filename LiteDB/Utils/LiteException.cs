using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// The main exception for LiteDB
    /// </summary>
    public class LiteException : Exception
    {
        public int ErrorCode { get; private set; }

        public LiteException(string message)
            : base(message)
        {
        }

        private LiteException(int code, string message, params object[] args)
            : base(string.Format(message, args))
        {
            this.ErrorCode = code;
        }

        #region Database Errors

        public static LiteException NoDatabase()
        {
            return new LiteException(100, "There is no database");
        }

        public static LiteException InvalidTransaction()
        {
            return new LiteException(101, "This operation doesn't support open transaction.");
        }

        public static LiteException FileNotFound(string fileId)
        {
            return new LiteException(102, "File '{0}' not found", fileId);
        }

        public static LiteException FileCorrupted(LiteFileInfo file)
        {
            return new LiteException(103, "File '{0}' has no content or is corrupted", file.Id);
        }

        public static LiteException InvalidDatabase(Stream stream)
        {
            var filename = stream is FileStream ? ((FileStream)stream).Name : "Stream";

            return new LiteException(104, "'{0}' is not a LiteDB database", filename);
        }

        public static LiteException InvalidDatabaseVersion(Stream stream, int version)
        {
            return new LiteException(105, "Invalid database version: {0}", version);
        }

        public static LiteException CollectionLimitExceeded(int limit)
        {
            return new LiteException(106, "This database exceeded the maximum limit of collections: {0}", limit);
        }

        public static LiteException JournalFileFound(string journal)
        {
            return new LiteException(107, "Journal file found on '{0}'. Reopen database", journal);
        }

        public static LiteException IndexDropId()
        {
            return new LiteException(108, "Primary key index '_id' can't be droped");
        }

        public static LiteException IndexLimitExceeded(string collection)
        {
            return new LiteException(109, "Collection '{0}' exceeded the maximum limit of indices: {1}", collection, CollectionIndex.INDEX_PER_COLLECTION);
        }

        public static LiteException IndexDuplicateKey(string field, BsonValue key)
        {
            return new LiteException(110, "Cannot insert duplicate key in unique index '{0}'. The duplicate value is '{1}'", field, key);
        }

        public static LiteException IndexKeyTooLong()
        {
            return new LiteException(111, "Index key must be less than {0} bytes", IndexService.MAX_INDEX_LENGTH);
        }

        public static LiteException LockTimeout(TimeSpan ts)
        {
            return new LiteException(112, "Timeout. Database is locked for more than {0}", ts.ToString());
        }

        public static LiteException InvalidCommand(string command)
        {
            return new LiteException(113, "Command '{0}' is not a valid shell command", command);
        }

        #endregion

        #region Document/Mapper Errors

        public static LiteException InvalidFormat(string field, string format)
        {
            return new LiteException(200, "Invalid format: {0}", field);
        }

        public static LiteException DocumentMaxDepth(int depth)
        {
            return new LiteException(201, "Document has more than {0} nested documents. Check for circular references", depth);
        }

        public static LiteException InvalidCtor(Type type)
        {
            return new LiteException(202, "Failed to create instance for type '{0}' from assembly '{1}'. Checks if has a public constructor with no parameters", type.FullName, type.AssemblyQualifiedName);
        }

        public static LiteException UnexpectedToken(string token)
        {
            return new LiteException(203, "Unexpected JSON token: {0}", token);
        }

        public static LiteException InvalidDataType(string field, BsonValue value)
        {
            return new LiteException(204, "Invalid BSON data type '{0}' on field '{1}'", value.Type, field);
        }

        public static LiteException PropertyReadWrite(PropertyInfo prop)
        {
            return new LiteException(205, "'{0}' property must have public get; set;", prop.Name);
        }

        public static LiteException PropertyNotMapped(string name)
        {
            return new LiteException(206, "Property '{0}' was not mapped into BsonDocument", name);
        }

        #endregion
    }
}
