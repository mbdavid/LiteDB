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
            this.RegisterSystemCollection("$database", () => this.SysDatabase());

            this.RegisterSystemCollection("$cols", () => this.SysCols());
            this.RegisterSystemCollection("$indexes", () => this.SysIndexes());

            this.RegisterSystemCollection("$dump", () => this.SysDumpData());
            this.RegisterSystemCollection("$dump_wal", () => this.SysDumpWal());

            this.RegisterSystemCollection("$sequences", () => this.SysSequences());

            this.RegisterSystemCollection("$transactions", () => this.SysTransactions());
            this.RegisterSystemCollection("$snapshots", () => this.SysSnapshots());
            this.RegisterSystemCollection("$versions", () => this.SysVersions());
            this.RegisterSystemCollection("$cursors", () => this.SysCursors());

            // external collections
            this.RegisterSystemCollection(new SysFileJson());
            this.RegisterSystemCollection(new SysFileCsv());

            this.RegisterSystemCollection(new SysQuery());
        }
    }
}