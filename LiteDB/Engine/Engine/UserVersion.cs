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
                using (var trans = this.BeginTrans())
                {
                    var header = trans.GetPage<HeaderPage>(0);

                    return header.UserVersion;
                }
            }
            set
            {
                using (var trans = this.BeginTrans())
                {
                    var header = trans.GetPage<HeaderPage>(0);

                    header.UserVersion = value;

                    // there is no explicit lock because use only header page - that will be locked inside this SetDirty()
                    trans.SetDirty(header);

                    // persist header change
                    trans.Commit();
                }
            }
        }
    }
}