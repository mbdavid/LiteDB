namespace LiteDB.Shell.Commands
{
    internal class Timer : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Match(@"timer$");
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            input.Timer.Start();
        }
    }
}