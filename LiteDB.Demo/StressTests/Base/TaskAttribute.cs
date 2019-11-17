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
    public class TaskAttribute : Attribute
    {
        /// <summary>
        /// Waiting time (in milliseconds) before first run
        /// </summary>
        public int InitialDelay { get; set; } = 2000;

        /// <summary>
        /// Repeat this method every N milliseconds
        /// </summary>
        public int Repeat { get; set; } = 1000;

        /// <summary>
        /// Random time (0-N) for initial/repeat tasks
        /// </summary>
        public int Random { get; set; } = 500;
    }
}
