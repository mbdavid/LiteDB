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
            var shell = new Shell();
            var input = new InputCommand();

            shell.Display.TextWriters.Add(Console.Out);

            // show welcome message
            shell.Welcome();

            // if has a argument, its database file - try open
            if (args.Length > 0)
            {
                try
                {
                    shell.Engine = new LiteEngine(args[0]);
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

                try
                {
                    // run it
                    shell.Run(cmd);
                }
                catch (Exception ex)
                {
                    shell.Display.WriteError(ex.Message);
                }
            }
        }
    }
}
