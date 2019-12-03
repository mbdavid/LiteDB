using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class ConcurrentCounter
    {
        private int _counter;

        public int Increment()
        {
            return Interlocked.Increment(ref _counter);
        }

        public int Decrement()
        {
            return Interlocked.Decrement(ref _counter);
        }
    }
}
