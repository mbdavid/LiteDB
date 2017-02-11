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

            LiteEngine engine = null;

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

                        // open datafile before execute
                        if (command.Access != DataAccess.None)
                        {
                            engine = env.CreateEngine(command.Access);
                        }

                        command.Execute(engine, s, display, input, env);

                        // close datafile to be always disconnected
                        if (engine != null)
                        {
                            engine.Dispose();
                            engine = null;
                        }

                        found = true;
                        break;
                    }

                    if (!found) throw new ShellException("Command not found");
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
            var types = typeof(ShellProgram).GetTypeInfo().Assembly
                .GetTypes()
                .Where(p => type.IsAssignableFrom(p) && p.GetTypeInfo().IsClass);

            foreach(var cmd in types)
            {
                commands.Add(Activator.CreateInstance(cmd) as ICommand);
            }
        }

        #endregion
    }
}