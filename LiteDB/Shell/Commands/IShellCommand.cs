namespace LiteDB.Shell
{
    internal interface IShellCommand
    {
        bool IsCommand(StringScanner s);

        BsonValue Execute(DbEngine engine, StringScanner s);
    }
}