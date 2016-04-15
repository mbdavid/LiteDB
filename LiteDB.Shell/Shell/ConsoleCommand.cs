using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    internal abstract class ConsoleCommand
    {
        public abstract bool IsCommand(StringScanner s);

        public abstract void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input);

        private static List<ConsoleCommand> _commands = new List<ConsoleCommand>();

        static ConsoleCommand()
        {
            var type = typeof(ConsoleCommand);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

            foreach (var t in types)
            {
                _commands.Add((ConsoleCommand)Activator.CreateInstance(t));
            }
        }

        /// <summary>
        /// If command is a console command, execute and returns true - if not, just returns false
        /// </summary>
        public static bool TryExecute(string command, ref IShellEngine engine, Display display, InputCommand input)
        {
            var s = new StringScanner(command);

            foreach (var cmd in _commands)
            {
                if (cmd.IsCommand(s))
                {
                    cmd.Execute(ref engine, s, display, input);
                    return true;
                }
            }

            return false;
        }
    }
}