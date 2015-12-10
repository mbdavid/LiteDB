namespace LiteDB.Shell.Commands
{
    internal class Close : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"close$").Length > 0;
        }

        public override void Execute(ref LiteDatabase db, StringScanner s, Display display, InputCommand input)
        {
            if (db == null) throw LiteException.NoDatabase();

            db.Dispose();

            db = null;
        }
    }
}