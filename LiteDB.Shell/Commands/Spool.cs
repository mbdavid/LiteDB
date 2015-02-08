using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Spool : ConsoleCommand
    {
        private TextWriter _writer;

        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"spo(ol)?\s*").Length > 0;
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
            if(s.Scan("false|off").Length > 0 && _writer != null)
            {
                display.TextWriters.Remove(_writer);
                input.OnWrite = null;
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
            else if(_writer == null)
            {
                if (shell.Database == null) throw new LiteException("No database");

                var dbfilename = shell.Database.ConnectionString.Filename;
                var path = Path.Combine(Path.GetDirectoryName(dbfilename),
                    string.Format("{0}-spool-{1:yyyy-MM-dd-HH-mm}.txt", Path.GetFileNameWithoutExtension(dbfilename), DateTime.Now));

                _writer = File.CreateText(path);

                display.TextWriters.Add(_writer);

                input.OnWrite = (t) => _writer.Write(t);
            }
        }
    }
}
