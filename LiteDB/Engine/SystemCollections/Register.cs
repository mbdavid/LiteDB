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
            this.RegisterSystemCollection("$cols", () => this.SysCols());
            this.RegisterSystemCollection("$database", () => this.SysDatabase());
            this.RegisterSystemCollection("$indexes", () => this.SysIndexes());
            this.RegisterSystemCollection("$dump", () => this.SysDumpData());
            this.RegisterSystemCollection("$dump_wal", () => this.SysDumpWal());
            this.RegisterSystemCollection("$snapshots", () => this.SysSnapshots());
            this.RegisterSystemCollection("$transactions", () => this.SysTransactions());
        }
    }
}