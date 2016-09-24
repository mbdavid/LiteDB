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
                    var header = _pager.GetPage<HeaderPage>(0, true);

                    header.UserVersion = value;

                    return true;
                });
            }
        }

        /// <summary>
        /// Increment/Decrement user version in atomic operation. Returns new UserVersion value
        /// </summary>
        public ushort UserVersionInc(ushort step = 1)
        {
            return this.Transaction<ushort>(null, false, (col) =>
            {
                var header = _pager.GetPage<HeaderPage>(0, true);

                header.UserVersion = (ushort)(header.UserVersion + step);

                return header.UserVersion;
            });

        }
    }
}