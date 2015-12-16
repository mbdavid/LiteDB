using System;
using System.Collections.Generic;
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

        // var dump = new DumpWriter(TextWriter)
        // var dump = new DumpReader(TextReader)
        //
        //void Export(DumpWriter dump, string[] collections);
        //void Import(DumpReader dump);
    }
}
