using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Run : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"run\s+").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            if (env.Engine == null) throw ShellException.NoDatabase();

            var filename = s.Scan(@".+").Trim();

            env.Input.Queue.Enqueue(File.ReadAllText(filename));
        }
    }
}