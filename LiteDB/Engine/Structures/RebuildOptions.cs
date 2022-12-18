using System;
using System.Collections.Generic;
using System.Text;

using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// </summary>
    public class RebuildOptions
    {
        /// <summary>
        /// Rebuild database with a new password
        /// </summary>
        public string Password { get; set; } = null;

        /// <summary>
        /// Define a new collation when rebuild
        /// </summary>
        public Collation Collation { get; set; } = null;

        /// <summary>
        /// When set true, if any problem occurs in rebuild, a $rebuild_errors collection
        /// will contains all errors found
        /// </summary>
        public bool IncludeErrorReport { get; set; } = true;

        /// <summary>
        /// After run rebuild process, get a error report (empty if no error detected)
        /// </summary>
        public IList<RebuildError> Errors { get; } = new List<RebuildError>();
    }
}