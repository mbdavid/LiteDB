namespace LiteDB.Shell.Commands
{
    internal class Version : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Match(@"ver(sion)?$");
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if(engine == null) throw ShellExpcetion.NoDatabase();

            var ver = engine.Version;

            display.WriteInfo(string.Format("v{0}.{1}.{2}",
                ver.Major,
                ver.Minor,
                ver.Build));
        }
    }
}