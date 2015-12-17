using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    interface IShellEngine : IDisposable
    {
        Version Version { get; }
        bool Detect(string filename);
        void Open(string connectionString);
        void Debug(bool enable);
        void Run(string command, Display display);
        void Dump(TextWriter writer);
    }
}
