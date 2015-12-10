namespace LiteDB.Shell.Commands
{
    internal class Open : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"open\s+").Length > 0;
        }

        public override void Execute(ref LiteDatabase db, StringScanner s, Display display, InputCommand input)
        {
            var filename = s.Scan(@".+");

            if (db != null)
            {
                db.Dispose();
            }

            db = new LiteDatabase(filename);
        }
    }
}