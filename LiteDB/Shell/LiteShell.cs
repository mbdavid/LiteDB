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
        }

        public LiteDatabase Database { get; set; }

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

        public BsonValue Run(string command)
        {
            if (string.IsNullOrEmpty(command)) return BsonValue.Null;

            var s = new StringScanner(command);

            foreach (var cmd in _commands)
            {
                if (cmd.IsCommand(s))
                {
                    return cmd.Execute(this.Database, s);
                }
            }

            throw new LiteException("Command ´" + command + "´ is not a valid command");
        }

        public void Dispose()
        {
            if (this.Database != null) this.Database.Dispose();
        }
    }
}
