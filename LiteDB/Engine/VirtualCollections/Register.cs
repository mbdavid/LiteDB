using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Register all internal virtual collections avaiable by default
        /// </summary>
        private void InitializeVirtualCollections()
        {
            this.RegisterVirtualCollection("$cols", () => this.GetCollectionNames().Select(x => new BsonDocument { ["name"] = x }));
            this.RegisterVirtualCollection("$dump", () => this.DumpDatafile());
        }
    }
}