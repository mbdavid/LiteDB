namespace LiteDB.Shell.Commands
{
    internal class FileDownload : BaseFileStorage, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "download");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var fs = new LiteFileStorage(engine);
            var id = this.ReadId(s);
            var filename = s.Scan(@"\s*.*").Trim();

            var file = fs.FindById(id);

            if (file != null)
            {
                file.SaveAs(filename, true);

                return file.AsDocument;
            }
            else
            {
                return false;
            }
        }
    }
}