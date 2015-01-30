using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Spool : IShellCommand
    {
        private TextWriter _writer;

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"spo(ol)?\s*").Length > 0;
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            if(s.Scan("false|off").Length > 0 && _writer != null)
            {
                display.TextWriters.Remove(_writer);
                _writer.Flush();
                _writer.Dispose();
                _writer = null;
            }
            else if(_writer == null)
            {
                if (db == null) throw new LiteException("No database");

                var path =
                    Path.Combine(Path.GetDirectoryName(db.ConnectionString.Filename),
                    string.Format("{0}-spool-{1:yyyy-MM-dd-HH-mm}.txt", Path.GetFileNameWithoutExtension(db.ConnectionString.Filename), DateTime.Now));

                _writer = File.CreateText(path);
                display.TextWriters.Add(_writer);
            }
        }
    }
}
