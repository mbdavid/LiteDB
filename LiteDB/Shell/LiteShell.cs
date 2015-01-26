using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    public class LiteShell : IDisposable
    {
        private List<IShellCommand> _commands = new List<IShellCommand>();

        public LiteShell()
        {
            this.Display = new Display();
        }

        public LiteShell(LiteEngine db, StringBuilder sb, bool pretty = true)
            : this()
        {
            this.Engine = db;

            var writer = new StringWriter(sb);
            this.Display.TextWriters.Add(writer);
            this.Display.Pretty = pretty;
            this.RegisterAll();
        }

        public LiteEngine Engine { get; set; }
        public Display Display { get; set; }

        /// <summary>
        /// Register all commands: search for all classes that implements IShellCommand
        /// </summary>
        public void RegisterAll()
        {
            _commands = new List<IShellCommand>();

            var type = typeof(IShellCommand);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);

            foreach (var t in types)
            {
                _commands.Add((IShellCommand)Activator.CreateInstance(t));
            }
        }

        public void Register<T>()
            where T : IShellCommand, new()
        {
            _commands.Add(new T());
        }

        public void Run(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            var s = new StringScanner(command);

            foreach (var cmd in _commands)
            {
                if (cmd.IsCommand(s))
                {
                    cmd.Execute(this.Engine, s, this.Display);
                    return;
                }
            }

            throw new ApplicationException("Command ´" + command + "´ is not a valid command");
        }

        public void Dispose()
        {
            if (this.Engine != null) this.Engine.Dispose();
        }
    }
}
