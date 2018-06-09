using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LiteDB.Shell.Commands;

namespace LiteDB.Shell
{
    internal class ShellProgram
    {
        public static void Start(InputCommand input, Display display)
        {
            var env = new Env(display, input);
            var commands = GetShellCommands().ToList();

            display.TextWriters.Add(Console.Out);

            // show welcome message
            display.WriteWelcome();

            while (input.Running)
            {
                // read next command from user or queue
                var cmd = input.ReadCommand();

                if (string.IsNullOrEmpty(cmd)) continue;

                try
                {
                    var s = new StringScanner(cmd);

                    var found = false;

                    // first test all shell app commands
                    foreach (var command in commands)
                    {
                        if (!command.IsCommand(s)) continue;

                        command.Execute(s, env);

                        found = true;
                        break;
                    }

                    // if not found, try "real" shell database command
                    if (!found)
                    {
                        var result = env.Engine.Execute(cmd, null);

                        env.Display.WriteResult(result);
                    }
                }
                catch (Exception ex)
                {
                    display.Write(ex);
                }
            }
        }

        #region Register all commands

        public static IEnumerable<IShellCommand> GetShellCommands()
        {
            var type = typeof(IShellCommand);
            var types = typeof(ShellProgram).Assembly
                .GetTypes()
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);

            foreach(var cmd in types)
            {
                yield return Activator.CreateInstance(cmd) as IShellCommand;
            }
        }

        #endregion
    }
}