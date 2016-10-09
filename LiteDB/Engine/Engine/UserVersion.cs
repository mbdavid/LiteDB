using System;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Get/Set User version internal database
        /// </summary>
        public ushort UserVersion
        {
            get
            {
                using (_locker.Read())
                {
                    var header = _pager.GetPage<HeaderPage>(0);

                    return header.UserVersion;
                }
            }
            set
            {
                this.Transaction<bool>(null, false, (col) =>
                {
                    var header = _pager.GetPage<HeaderPage>(0);

                    header.UserVersion = value;

                    _pager.SetDirty(header);

                    return true;
                });
            }
        }
    }
}