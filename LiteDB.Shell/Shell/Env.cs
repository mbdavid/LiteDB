using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Shell
{
    public class Env
    {
        public string Filename { get; set; }
        public string Password { get; set; }
        public bool Journal { get; set; }
        public Logger Log { get; set; }

        public Env()
        {
            this.Log = new Logger();
            this.Log.Logging += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(msg);
            };
        }

        public LiteEngine CreateEngine(DataAccess access)
        {
            if (this.Filename == null) throw new ShellExpcetion("No database");

            var disk = new FileDiskService(this.Filename,
                new FileOptions
                {
                    FileMode = FileMode.Shared,
                    Journal = this.Journal
                });

            return new LiteEngine(disk, password: this.Password, log: this.Log);
        }
    }
}