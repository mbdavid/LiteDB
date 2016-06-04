using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal partial class DbEngine : IDisposable
    {
        /// <summary>
        /// Read DbVersion variable from header page
        /// </summary>
        public ushort ReadDbVersion()
        {
            return 0;
        }

        /// <summary>
        /// Write DbVersion variable in header page
        /// </summary>
        /// <param name="version"></param>
        public void WriteDbVersion(ushort version)
        {
            //TODO
        }
    }
}