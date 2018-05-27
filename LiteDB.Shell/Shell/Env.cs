using LiteDB.Engine;
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
        public EngineSettings Settings { get; private set; }


        public Env(Display display, InputCommand input)
        {
            this.Display = display;
            this.Input = input;

            this.Settings = new EngineSettings
            {
                Log = new Logger(Logger.FULL, (m) => this.Display.WriteLine(ConsoleColor.DarkGreen, m))
            };
        }

        public void Open(string filename)
        {
            this.Close();

            this.Settings.Filename = filename;

            this.Engine = new LiteEngine(this.Settings);
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