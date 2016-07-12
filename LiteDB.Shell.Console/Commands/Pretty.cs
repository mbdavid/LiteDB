namespace LiteDB.Shell.Commands
{
    internal class Pretty : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"pretty\s*").Length > 0;
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            display.Pretty = !(s.Scan(@"off\s*").Length > 0);
        }
    }
}