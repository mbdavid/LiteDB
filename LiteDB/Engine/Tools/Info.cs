using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Get all database information
        /// </summary>
        public BsonObject GetDatabaseInfo()
        {
            this.Transaction.AvoidDirtyRead();

            var info = new BsonObject();

            info["filename"] = this.ConnectionString.Filename;
            info["journal"] = this.ConnectionString.JournalEnabled;
            info["timeout"] = this.ConnectionString.Timeout.TotalSeconds;
            info["version"] = this.Cache.Header.UserVersion;
            info["changeID"] = this.Cache.Header.ChangeID;
            info["fileLength"] = (this.Cache.Header.LastPageID + 1) * BasePage.PAGE_SIZE;
            info["lastPageID"] = this.Cache.Header.LastPageID;
            info["pagesInCache"] = this.Cache.PagesInCache;
            info["dirtyPages"] = this.Cache.GetDirtyPages().Count();

            return info;
        }
    }
}
