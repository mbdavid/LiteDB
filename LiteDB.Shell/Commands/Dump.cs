using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Dump : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"dump\s+").Length > 0;
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if (engine == null) throw ShellExpcetion.NoDatabase();

            var filename = s.Scan(@".+").Trim();

            using (var file = new FileStream(filename, FileMode.Create))
            {
                using (var writer = new StreamWriter(file))
                {
                    engine.Dump(writer);
                    writer.Flush();
                }
            }
        }
    }
}