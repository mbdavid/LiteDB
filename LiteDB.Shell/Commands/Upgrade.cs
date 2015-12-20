using System;
using System.IO;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Upgrade : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"upgrade").Length > 0;
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if (engine == null) throw ShellExpcetion.NoDatabase();

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                engine.Dump(writer);
            }

            var eng = new ShellEngine_200();
            eng.Open("my_new_db.db");

            foreach(var line in sb.ToString().Split('\n'))
            {
                if (line.StartsWith("--")) continue;
                eng.Run(line, new Display()); // no output
            }
        }
    }
}