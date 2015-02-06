using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    class Program
    {
        static void Main(string[] args)
        {
            var shell = new LiteShell();
            var input = new InputCommand();
            var display = new Display();

            shell.RegisterAll();
            display.TextWriters.Add(Console.Out);

            // show welcome message
            display.WriteWelcome();

            // if has a argument, its database file - try open
            if (args.Length > 0)
            {
                try
                {
                    shell.Database = new LiteDatabase(args[0]);
                }
                catch (Exception ex)
                {
                    display.WriteError(ex.Message);
                }
            }

            while (true)
            {
                // read next command from user
                var cmd = input.ReadCommand();

                if (string.IsNullOrEmpty(cmd)) continue;

                try
                {
                    var isConsoleCommand = ConsoleCommand.TryExecute(cmd, shell, display, input);

                    if (isConsoleCommand == false)
                    {
                        var result = shell.Run(cmd);

                        display.WriteResult(result);
                    }
                }
                catch (Exception ex)
                {
                    display.WriteError(ex.Message);
                }
            }
        }

        static void Open(LiteShell shell, string command, Display display)
        {
            if (shell.Database != null)
            {
                shell.Database.Dispose();
            }

            shell.Database = new LiteDatabase(command.Substring(5));
        }

        static void Spool(LiteShell shell, string command, Display display)
        {
        }

        static void Help(Display display)
        {
        }
    }
}
