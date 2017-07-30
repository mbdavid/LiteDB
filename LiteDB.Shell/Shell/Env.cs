using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Shell
{
    public class Env
    {
        public ConnectionString ConnectionString { get; set; }
        public bool LogEnabled { get; set; }

        public LiteEngine CreateEngine(DataAccess access)
        {
            if (this.ConnectionString == null) throw new ShellException("No database");

            var db = new LiteDatabase(this.ConnectionString);

            db.Log.Level = this.LogEnabled ? Logger.FULL : Logger.NONE;

            db.Log.Logging += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(msg);
            };

            return db.Engine;
        }
    }
}