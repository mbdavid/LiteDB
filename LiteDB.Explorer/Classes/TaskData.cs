using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiteDB.Explorer
{
    class TaskData
    {
        public int Id { get; set; }
        public bool Running { get; set; } = false;
        public string Sql { get; set; } = "";
        public List<BsonValue> Result { get; set; } = null;
        public Exception Exception { get; set; } = null;
        public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;

        public Thread Thread { get; set; }
    }
}
