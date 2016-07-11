namespace LiteDB.Shell.Commands
{
    internal class Close : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"close$").Length > 0;
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if (engine == null) throw ShellExpcetion.NoDatabase();

            engine.Dispose();

            engine = null;
        }
    }
}