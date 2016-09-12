using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    internal class ShellProgram
    {
        public static void Start(InputCommand input, Display display)
        {
            LiteEngine engine = null;
            var commands = new List<ICommand>();

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
                    var found = false;
                    var s = new StringScanner(cmd);

                    foreach(var command in commands)
                    {
                        if (command.IsCommand(s))
                        {
                            var shell = command as IShellCommand;
                            var console = command as IConsoleCommand;

                            if (shell != null) shell.Execute(engine, s);
                            if (console != null) console.Execute(ref engine, s, display, input);

                            found = true;
                            break;
                        }
                    }

                    if (!found) throw new ShellExpcetion("Command not found");
                }
                catch (Exception ex)
                {
                    display.WriteError(ex.Message);
                }
            }
        }

        public static void LogMessage(string msg)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine(msg);
        }

        #region Register all commands

        public static void RegisterCommands(List<ICommand> commands)
        {
            //TODO: register all commands (shell + console)
        }

        #endregion
    }
}