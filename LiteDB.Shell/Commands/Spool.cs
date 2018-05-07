using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Shell",
        Name = "spool",
        Syntax = "spool [off]",
        Description = "Starts spool all output to a disk file. Use off keyword to stop."
    )]
    internal class Spool : IShellCommand
    {
        private TextWriter _writer;

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"spo(ol)?\s*").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            if (s.Scan("false|off").Length > 0 && _writer != null)
            {
                env.Display.TextWriters.Remove(_writer);
                env.Input.OnWrite = null;
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
            else if (_writer == null)
            {
                if (env.Engine == null) throw ShellException.NoDatabase();

                var path = Path.GetFullPath(string.Format("LiteDB-spool-{0:yyyy-MM-dd-HH-mm}.txt", DateTime.Now));

                _writer = File.CreateText(path);

                env.Display.TextWriters.Add(_writer);

                env.Input.OnWrite = (t) => _writer.Write(t);
            }
        }
    }
}