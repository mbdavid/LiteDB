using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Debug : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"debug\s*").Length > 0;
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display d, InputCommand input)
        {
            var sb = new StringBuilder();
            var enabled = !(s.Scan(@"off\s*").Length > 0);

            if(engine == null) throw ShellExpcetion.NoDatabase();

            engine.Debug(enabled);
        }
    }
}