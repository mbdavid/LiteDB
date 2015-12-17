using System;
using System.IO;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Upgrade : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"upgrade\s+").Length > 0;
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if (engine == null) throw ShellExpcetion.NoDatabase();

            var filename = s.Scan(@".+").Trim();

            // dump current database to a string builder
            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            {
                engine.Dump(writer);
            }

            // open new database
            input.Queue.Enqueue("open " + filename);

            // enqueue all commands to re-create database
            foreach (var line in sb.ToString().Split('\n'))
            {
                input.Queue.Enqueue(line);
            }
        }
    }
}