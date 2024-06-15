namespace LiteDB.Shell;

using System;
using System.Collections.Generic;

public class InputCommand
{
    public Queue<string> Queue { get; set; }
    public List<string> History { get; set; }
    public bool Running { get; set; }
    public bool AutoExit { get; set; }

    public InputCommand()
    {
        Queue = new Queue<string>();
        History = new List<string>();
        Running = true;
        AutoExit = false; // run "exit" command when there is not more command in queue
    }

    public string ReadCommand()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("> ");

        var cmd = ReadLine();

        if (cmd == null)
        {
            AutoExit = true;
            Running = false;
            return "";
        }

        cmd = cmd.Trim();

        // single line only for shell commands
        if (ShellProgram.GetCommand(cmd) == null)
        {
            while (!cmd.EndsWith(";"))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("| ");

                var line = ReadLine();
                cmd += Environment.NewLine + line;
            }
        }

        cmd = cmd.Trim();

        History.Add(cmd);

        return cmd.Trim();
    }

    /// <summary>
    ///     Read a line from queue or user
    /// </summary>
    private string ReadLine()
    {
        Console.ForegroundColor = ConsoleColor.Gray;

        if (Queue.Count > 0)
        {
            var cmd = Queue.Dequeue();
            Console.Write(cmd + Environment.NewLine);
            return cmd;
        }

        if (AutoExit)
            return "exit";

        return Console.ReadLine();
    }
}