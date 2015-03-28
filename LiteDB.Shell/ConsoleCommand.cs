using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    public abstract class ConsoleCommand
    {
        public abstract bool IsCommand(StringScanner s);
        public abstract void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input);

        private static List<ConsoleCommand> Commands = new List<ConsoleCommand>();

        static ConsoleCommand()
        {
            var type = typeof(ConsoleCommand);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);

            foreach (var t in types)
            {
                Commands.Add((ConsoleCommand)Activator.CreateInstance(t));
            }
        }

        /// <summary>
        /// If command is a console command, execute and returns true - if not, just returns false
        /// </summary>
        public static bool TryExecute(string command, LiteShell shell, Display display, InputCommand input)
        {
            var s = new StringScanner(command);

            foreach (var cmd in Commands)
            {
                if (cmd.IsCommand(s))
                {
                    cmd.Execute(shell, s, display, input);
                    return true;
                }
            }

            return false;
        }
    }
}
