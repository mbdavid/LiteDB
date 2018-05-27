using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB.Shell
{
    internal class Env
    {
        public Display Display { get; private set; }
        public InputCommand Input { get; private set; }
        public LiteEngine Engine { get; private set; }

        private LiteEngine _engine = null;

        public Env(Display display, InputCommand input)
        {
            this.Display = display;
            this.Input = input;
        }

        public void Open(string connectionString)
        {
            this.Close();

            this.Engine = new LiteEngine(connectionString);
        }

        public void Close()
        {
            if (this.Engine != null)
            {
                this.Engine.Dispose();
                this.Engine = null;
            }
        }
    }
}