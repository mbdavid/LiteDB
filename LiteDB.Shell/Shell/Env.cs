using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Shell
{
    public class Env
    {
        public ConnectionString ConnectionString { get; set; }
        public Logger Log { get; set; }

        public Env()
        {
            this.ConnectionString = null;
            this.Log = new Logger();
            this.Log.Logging += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(msg);
            };
        }

        public LiteEngine CreateEngine(DataAccess access)
        {
            if (this.ConnectionString == null) throw new ShellException("No database");

            var db = new LiteDatabase(this.ConnectionString);

            return db.Engine;
        }
    }
}