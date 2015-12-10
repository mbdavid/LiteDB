namespace LiteDB.Shell.Commands
{
    internal class FileDelete : BaseFileStorage, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "delete");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var fs = new LiteFileStorage(engine);
            var id = this.ReadId(s);

            return fs.Delete(id);
        }
    }
}