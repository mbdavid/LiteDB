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
        public bool Log { get; set; }

        public LiteEngine CreateEngine(DataAccess access)
        {
            if (this.Filename == null) throw new ShellExpcetion("No database");

            var disk = new FileDiskService(this.Filename,
                new FileOptions
                {
                    ReadOnly = access == DataAccess.Read,
                    Journal = this.Journal
                });

            var engine = new LiteEngine(disk, this.Password);

            if (this.Log)
            {
                engine.Log.Level = Logger.FULL;
                engine.Log.Logging += (msg) =>
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine(msg);
                };
            }

            return engine;
        }
    }
}