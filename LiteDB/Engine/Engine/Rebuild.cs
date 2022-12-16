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
        /// Implement a full database export/import. Should run with database closed
        /// </summary>
        public long Rebuild(RebuildOptions options)
        {
            if (_isOpen) throw LiteException.InvalidEngineState(false, "REBUILD");

            // create a new error report 
            options.ErrorReport = new StringBuilder();

            var rebuilder = new RebuildService(_settings);

            // return how many bytes of diference from original/rebuild version
            return rebuilder.Rebuild(options);
        }
    }
}