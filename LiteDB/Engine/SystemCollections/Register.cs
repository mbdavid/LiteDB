using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Register all internal system collections avaiable by default
        /// </summary>
        private void InitializeSystemCollections()
        {
            this.RegisterSystemCollection("$database", p => this.SysDatabase());

            this.RegisterSystemCollection("$cols", p => this.SysCols());
            this.RegisterSystemCollection("$indexes", p => this.SysIndexes());

            this.RegisterSystemCollection("$dump", p => this.SysDumpData());
            this.RegisterSystemCollection("$dump_wal", p => this.SysDumpWal());

            this.RegisterSystemCollection("$sequences", p => this.SysSequences());
            this.RegisterSystemCollection("$transactions", p => this.SysTransactions());
            this.RegisterSystemCollection("$snapshots", p => this.SysSnapshots());
            this.RegisterSystemCollection("$open_cursors", p => this.SysOpenCursors());

            // external collections
            this.RegisterSystemCollection(new SysFileJson());
        }
    }
}