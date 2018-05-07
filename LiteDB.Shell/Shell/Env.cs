using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Shell
{
    internal class Env
    {
        public Display Display { get; set; }
        public InputCommand Input { get; set; }
        public Logger Log { get; set; }

        private LiteEngine _engine = null;

        public Env()
        {
            this.Log = new Logger(Logger.NONE, (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(msg);
            });
        }

        public LiteEngine Engine
        {
            get
            {
                if (_engine == null) throw ShellException.NoDatabase();
                return _engine;
            }
        }

        public void Open(ConnectionString connectionString)
        {
            this.Close();

            _engine = new LiteDatabase(connectionString, null, this.Log).Engine;
        }

        public void Close()
        {
            if (_engine != null)
            {
                _engine.Dispose();
                _engine = null;
            }
        }
    }
}