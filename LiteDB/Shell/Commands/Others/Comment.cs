namespace LiteDB.Shell.Commands
{
    public class Comment : IShellCommand
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