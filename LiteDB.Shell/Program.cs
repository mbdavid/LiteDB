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

            shell.RegisterAll();
            shell.Display.TextWriters.Add(Console.Out);

            // show welcome message
            shell.Display.WriteWelcome();

            // if has a argument, its database file - try open
            if (args.Length > 0)
            {
                try
                {
                    shell.Database = new LiteDatabase(args[0]);
                }
                catch (Exception ex)
                {
                    shell.Display.WriteError(ex.Message);
                }
            }

            while (true)
            {
                // read next command from user
                var cmd = input.ReadCommand();

                if (string.IsNullOrEmpty(cmd)) continue;

                try
                {
                    if (cmd.StartsWith("open "))
                    {
                        if (shell.Database != null)
                        {
                            shell.Database.Dispose();
                        }

                        shell.Database = new LiteDatabase(cmd.Substring(5));
                    }
                    else
                    {
                        shell.Run(cmd);
                    }
                }
                catch (Exception ex)
                {
                    shell.Display.WriteError(ex.Message);
                }
            }
        }
    }
}
