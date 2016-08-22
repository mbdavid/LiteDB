using System;

namespace LiteDB
{
    internal partial class Engine
    {
        /// <summary>
        /// Read DbVersion variable from header page
        /// </summary>
        public ushort ReadDbVersion()
        {
            var header = _pager.GetPage<HeaderPage>(0);

            return header.DbVersion;
        }

        /// <summary>
        /// Write DbVersion variable in header page
        /// </summary>
        public void WriteDbVersion(ushort version)
        {
            this.Transaction<bool>(null, false, (col) =>
            {
                var header = _pager.GetPage<HeaderPage>(0, true);

                header.DbVersion = version;

                return true;
            });
        }
    }
}