using System;
using System.Collections.Generic;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Get database user version from collection page list
        /// </summary>
        public int GetUserVersion(LiteTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            return transaction.CreateSnapshot(SnapshotMode.Read, snapshot =>
            {
                var colList = snapshot.GetPage<CollectionListPage>(1);

                return colList.UserVersion;
            });
        }

        /// <summary>
        /// Set database user version from collection page list. Support new value or increment value. Return new UserVersion value
        /// </summary>
        public int SetUserVersion(int? newValue, int? increment, LiteTransaction transaction)
        {
            if (newValue == null && increment == null) throw new ArgumentNullException(string.Format("Both {0} and {1} parameters cann't be null", nameof(newValue), nameof(increment)));
            if (newValue != null && increment != null) throw new ArgumentNullException(string.Format("Only one parameter can have value: {0} or {1}", nameof(newValue), nameof(increment)));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            return transaction.CreateSnapshot(SnapshotMode.Write, snapshot =>
            {
                var colList = snapshot.GetPage<CollectionListPage>(1);

                if (newValue.HasValue)
                {
                    colList.UserVersion = newValue.Value;
                }
                else
                {
                    colList.UserVersion += increment.Value;
                }

                snapshot.SetDirty(colList);

                return colList.UserVersion;
            });
        }
    }
}