using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace LiteDB.Stress
{
    public class TestExecution
    {
        public TimeSpan Duration { get; }
        public Stopwatch Timer { get; }

        public ConcurrentDictionary<int, ThreadInfo> _threads = new ConcurrentDictionary<int, ThreadInfo>();

        public TestExecution(string filename, TimeSpan duration)
        {
            this.Duration = duration;

            var f = new TestFile(filename);


        }

        public void Execute()
        {
        }
    }
}
