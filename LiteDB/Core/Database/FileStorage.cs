using System;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        private LiteFileStorage _fs = null;

        /// <summary>
        /// Returns a special collection for storage files/stream inside datafile
        /// </summary>
        public LiteFileStorage FileStorage
        {
            get { return _fs ?? (_fs = new LiteFileStorage(_engine.Value)); }
        }
    }
}