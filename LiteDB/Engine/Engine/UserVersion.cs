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
                using (var trans = this.ReadTransaction(null))
                {
                    var header = trans.Pager.GetPage<HeaderPage>(0);

                    return header.UserVersion;
                }
            }
            set
            {
                this.WriteTransaction(TransactionMode.Reserved, null, false, trans =>
                {
                    var header = trans.Pager.GetPage<HeaderPage>(0);

                    header.UserVersion = value;

                    // there is no explicit lock because use only header page - that will be locked inside this SetDirty()
                    trans.Pager.SetDirty(header);

                    return true;
                });
            }
        }
    }
}