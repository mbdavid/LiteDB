using System;
using System.IO;
using System.Text;

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

            var direction = s.Scan(@"[><]\s*").Trim();
            var filename = s.Scan(@".+").Trim();

            //dump import
            if(direction == "<")
            {
                using (var reader = new StreamReader(filename, Encoding.UTF8))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        engine.Run(line, new Display()); // no output
                    }
                }
            }
            // dump export
            else
            {
                using (var writer = new StreamWriter(filename, false, Encoding.UTF8, 65536))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine("-- LiteDB v{0}.{1}.{2} dump file @ {3}", 
                        engine.Version.Major, engine.Version.Minor, engine.Version.Build,
                        DateTime.Now);
                    engine.Dump(writer);
                    writer.Flush();
                }
            }
        }
    }
}