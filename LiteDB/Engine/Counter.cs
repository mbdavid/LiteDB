using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public partial class LiteEngine : ILiteEngine
    {
        public readonly static Counter GET_NODE_DISK = new Counter();
        public readonly static Counter GET_NODE_CACHE = new Counter();
        public readonly static Counter COMPARE = new Counter();
    }

    public class Counter
    {
        public Stopwatch Stopwatch { get; } = new Stopwatch();
        public long Count { get; set; } = 0;

        public void StartInc()
        {
            this.Stopwatch.Start();
            this.Count++;
        }

        public void Stop()
        {
            this.Stopwatch.Stop();
        }

        public void Reset()
        {
            this.Stopwatch.Stop();
            this.Stopwatch.Reset();
            this.Count = 0;
        }
    }
}
