using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        // Parameters names
        private const string MAX_FILE_LENGTH = "maxFileLength";
        private const string INDEX_IGNORE_CASE = "indexIgnoreCase";
        private const string INDEX_WHITESPACE_TO_NULL = "indexWhiteSpaceToNull";

        /// <summary>
        /// Get all database information
        /// </summary>
        public BsonObject GetDatabaseInfo()
        {
            this.Transaction.AvoidDirtyRead();

            var info = new BsonObject();
            var param = new BsonObject();

            info["filename"] = this.ConnectionString.Filename;
            info["journal"] = this.ConnectionString.JournalEnabled;
            info["timeout"] = this.ConnectionString.Timeout.TotalSeconds;
            info["version"] = this.Cache.Header.UserVersion;
            info["changeID"] = this.Cache.Header.ChangeID;
            info["fileLength"] = (this.Cache.Header.LastPageID + 1) * BasePage.PAGE_SIZE;
            info["lastPageID"] = this.Cache.Header.LastPageID;
            info["pagesInCache"] = this.Cache.PagesInCache;
            info["dirtyPages"] = this.Cache.GetDirtyPages().Count();

            // parameters info
            param[MAX_FILE_LENGTH] = this.Cache.Header.MaxFileLength == long.MaxValue ? BsonValue.Null : new BsonValue(this.Cache.Header.MaxFileLength);
            param[INDEX_IGNORE_CASE] = true;
            param[INDEX_WHITESPACE_TO_NULL] = true;

            info["parameters"] = param;

            return info;
        }

        /// <summary>
        /// Change internal database parameters - this changes are persistable in datafile
        /// </summary>
        public void SetParameter(string paramName, object value)
        {
            if (this.Transaction.IsInTransaction)
            {
                throw new LiteException("Change parameter is a non transaction operation.");
            }

            this.Transaction.Begin();

            try
            {
                switch (paramName)
                {
                    case MAX_FILE_LENGTH:
                        var size = value == null ? Int64.MaxValue : Convert.ToInt64(value);
                        var min = (this.Cache.Header.LastPageID + 1) * BasePage.PAGE_SIZE;

                        if (size < (256 * 1024)) throw new ArgumentException(paramName + " must be bigger than 262.144 (256Kb)");
                        if (size < min) throw new ArgumentException(paramName + " must be bigger than " + min);

                        if (this.Cache.Header.MaxFileLength == size)
                        {
                            this.Transaction.Abort();
                            return;
                        }

                        this.Cache.Header.MaxFileLength = size;
                        break;
                    case INDEX_IGNORE_CASE:
                        throw new NotImplementedException();

                    case INDEX_WHITESPACE_TO_NULL:
                        throw new NotImplementedException();

                    default: throw new LiteException("Invalid setting name");
                }

                this.Cache.Header.IsDirty = true;
                this.Transaction.Commit();

            }
            catch (Exception ex)
            {
                this.Transaction.Rollback();
                throw ex;
            }
        }
    }
}
