namespace LiteDB.Shell.Commands
{
    internal class Shrink : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"shrink$").Length > 0;
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            return engine.Shrink();
        }
    }
}