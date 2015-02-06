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
        private List<ILiteCommand> _commands = new List<ILiteCommand>();

        public LiteShell()
        {
            this.Display = new Display();
        }

        public LiteShell(LiteDatabase db, StringBuilder sb, bool pretty = true)
            : this()
        {
            this.Database = db;

            var writer = new StringWriter(sb);
            this.Display.TextWriters.Add(writer);
            this.Display.Pretty = pretty;
            this.RegisterAll();
        }

        public LiteDatabase Database { get; set; }
        public Display Display { get; set; }

        /// <summary>
        /// Register all commands: search for all classes that implements IShellCommand
        /// </summary>
        public void RegisterAll()
        {
            _commands = new List<ILiteCommand>();

            var type = typeof(ILiteCommand);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);

            foreach (var t in types)
            {
                _commands.Add((ILiteCommand)Activator.CreateInstance(t));
            }
        }

        public void Register<T>()
            where T : ILiteCommand, new()
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
                    cmd.Execute(this.Database, s, this.Display);
                    return;
                }
            }

            throw new ApplicationException("Command ´" + command + "´ is not a valid command");
        }

        public void Dispose()
        {
            if (this.Database != null) this.Database.Dispose();
        }
    }
}
