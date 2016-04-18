namespace LiteDB.Shell.Commands
{
    internal class Comment : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"--");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            return BsonValue.Null;
        }
    }
}