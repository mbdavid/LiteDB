using System;

namespace LiteDB.Shell
{
    internal class ShellProgram
    {
        public static void Start(string filename)
        {
            IShellEngine engine = null;
            var input = new InputCommand();
            var display = new Display();

            display.TextWriters.Add(Console.Out);

            // show welcome message
            display.WriteWelcome();

            // if has filename, open
            if (!string.IsNullOrEmpty(filename))
            {
                input.Queue.Enqueue("open " + filename);
            }

            while (true)
            {
                // read next command from user
                var cmd = input.ReadCommand();

                if (string.IsNullOrEmpty(cmd)) continue;

                try
                {
                    var isConsoleCommand = ConsoleCommand.TryExecute(cmd, ref engine, display, input);

                    if (isConsoleCommand == false)
                    {
                        if (engine == null) throw ShellExpcetion.NoDatabase();

                        engine.Run(cmd, display);
                    }
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
    }
}