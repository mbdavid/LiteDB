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
    public class Log
    {
        public int Id { get; set; }
        public string Task { get; set; }
        public int? Thread { get; set; }
        public int Timer { get; set; }
        public int Delay { get; set; }
        public double Elapsed { get; set; }
        public int Concurrent { get; set; }
        public string Error { get; set; }
    }
}
