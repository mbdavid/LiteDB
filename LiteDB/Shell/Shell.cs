using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    public class Shell : IDisposable
    {
        private List<ICommand> _commands;
        private LiteEngine _engine;

        public Shell()
        {
            this.Display = new Display();
            this.LoadCommands();
        }

        public LiteEngine Engine { get { return _engine; } set { _engine = value; } }
        public Display Display { get; set; }
        public bool WebMode { get; set; }

        private void LoadCommands()
        {
            _commands = new List<ICommand>();

            var type = typeof(ICommand);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);

            foreach (var t in types)
            {
                _commands.Add((ICommand)Activator.CreateInstance(t));
            }
        }

        public void Welcome()
        {
            this.Display.WriteInfo("Welcome to LiteDB Shell");
            this.Display.WriteInfo("");
            this.Display.WriteInfo("Getting started with `help`");
            this.Display.WriteInfo("");
        }

        public void Run(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            var s = new StringScanner(command);

            foreach (var cmd in _commands)
            {
                if (cmd.IsCommand(s))
                {
                    var type = cmd.GetType();

                    if (_engine == null && !typeof(IShellCommand).IsAssignableFrom(type))
                    {
                        throw new ApplicationException("No database");
                    }
                    else if (this.WebMode && !typeof(IWebCommand).IsAssignableFrom(type))
                    {
                        throw new ApplicationException("ERROR: This command are not avaiable in web shell");
                    }

                    cmd.Execute(ref _engine, s, this.Display);
                    return;
                }
            }

            throw new ApplicationException("ReferenceError: ´" + command + "´ is not defined");
        }

        public void Dispose()
        {
            if (_engine != null) _engine.Dispose();
        }
    }
}
