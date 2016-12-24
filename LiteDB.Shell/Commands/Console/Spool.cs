using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Spool : ICommand
    {
        private TextWriter _writer;

        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"spo(ol)?\s*").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            if (s.Scan("false|off").Length > 0 && _writer != null)
            {
                display.TextWriters.Remove(_writer);
                input.OnWrite = null;
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
            else if (_writer == null)
            {
                if (engine == null) throw ShellException.NoDatabase();

                var path = Path.GetFullPath(string.Format("LiteDB-spool-{0:yyyy-MM-dd-HH-mm}.txt", DateTime.Now));

                _writer = File.CreateText(path);

                display.TextWriters.Add(_writer);

                input.OnWrite = (t) => _writer.Write(t);
            }
        }
    }
}