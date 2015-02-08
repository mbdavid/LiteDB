using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteDatabase
    {
        /// <summary>
        /// Virtual method for update database when a new version (from coneection string) was setted
        /// </summary>
        /// <param name="newVersion">The new database version</param>
        protected virtual void OnVersionUpdate(int newVersion)
        {
        }

        /// <summary>
        /// Update database version, when necessary
        /// </summary>
        private void UpdateDatabaseVersion()
        {
            // not necessary "AvoidDirtyRead" because its calls from ctor
            var current = this.Cache.Header.UserVersion;
            var recent = this.ConnectionString.UserVersion;

            // there is no updates
            if (current == recent) return;

            // start a transaction
            this.Transaction.Begin();

            try
            {
                for (var newVersion = current + 1; newVersion <= recent; newVersion++)
                {
                    OnVersionUpdate(newVersion);

                    this.Cache.Header.UserVersion = newVersion;
                }

                this.Cache.Header.IsDirty = true;
                this.Transaction.Commit();

            }
            catch
            {
                this.Transaction.Rollback();
                throw;
            }
        }
    }
}
