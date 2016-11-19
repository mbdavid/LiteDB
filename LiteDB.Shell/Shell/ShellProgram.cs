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
            var commands = new List<ICommand>();
            var env = new Env();

            // register commands
            RegisterCommands(commands);

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

                    // test all commands
                    foreach (var command in commands)
                    {
                        if (!command.IsCommand(s)) continue;

                        // test if command it's only shell command
                        if (command.Access == DataAccess.None)
                        {
                            command.Execute(null, s, display, input, env);
                        }
                        else
                        {
                            using (var engine = env.CreateEngine(command.Access))
                            {
                                command.Execute(engine, s, display, input, env);
                            }
                        }

                        found = true;
                        break;
                    }

                    if (!found) throw new ShellExpcetion("Command not found");
                }
                catch (Exception ex)
                {
                    display.WriteError(ex.Message);
                }
            }
        }

        #region Register all commands

        public static void RegisterCommands(List<ICommand> commands)
        {
            var type = typeof(ICommand);
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);

            foreach(var cmd in types)
            {
                commands.Add(Activator.CreateInstance(cmd) as ICommand);
            }
        }

        #endregion
    }
}