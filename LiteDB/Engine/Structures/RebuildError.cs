using System;
using System.Collections.Generic;
using System.Text;

using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// </summary>
    public class RebuildError
    {
        public DateTime Created { get; } = DateTime.Now;
        public uint PageID { get; set; }
        public int Code { get; set; }
        public string Field { get; set; }
        public string Message { get; set; }
    }
}