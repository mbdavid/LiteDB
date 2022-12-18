using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implement a full database export/import. Database should be closed before Rebuild
        /// </summary>
        public long Rebuild(RebuildOptions options)
        {
            if (_isOpen) throw LiteException.InvalidEngineState(false, "REBUILD");

            // run build service
            var rebuilder = new RebuildService(_settings);

            // create a new error report 
            options.Errors.Clear();

            // return how many bytes of diference from original/rebuild version
            return rebuilder.Rebuild(options);
        }
    }
}