using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Define how document will be update using [modify] expression
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>
        /// Current document will be merge with modify expression
        /// </summary>
        Merge,

        /// <summary>
        /// Current document wll be replaced with modify expression
        /// </summary>
        Replace
    }
}
