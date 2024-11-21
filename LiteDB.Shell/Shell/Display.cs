namespace LiteDB.Shell;

using System;

internal class Display
{
    public bool Pretty { get; set; }

    public Display()
    {
        Pretty = false;
    }

    public void WriteWelcome()
    {
        WriteInfo("Welcome to LiteDB Shell");
        WriteInfo("");
        WriteInfo("Getting started with `help`");
        WriteInfo("");
    }

    public void WritePrompt(string text)
    {
        Write(ConsoleColor.White, text);
    }

    public void WriteInfo(string text)
    {
        WriteLine(ConsoleColor.Gray, text);
    }

    public void WriteError(Exception ex)
    {
        WriteLine(ConsoleColor.Red, ex.Message);

        if (ex is LiteException liteEx && liteEx.ErrorCode == LiteException.UNEXPECTED_TOKEN)
        {
            WriteLine(ConsoleColor.DarkYellow, "> " + "^".PadLeft((int)liteEx.Position + 1, ' '));
        }
    }

    public void WriteResult(IBsonDataReader result, Env env)
    {
        var index = 0;
        var writer = new JsonWriter(Console.Out)
        {
            Pretty = Pretty,
            Indent = 2
        };

        foreach (var item in result.ToEnumerable())
        {
            if (env.Running == false)
                return;

            Write(ConsoleColor.Cyan, string.Format("[{0}]: ", ++index));

            if (Pretty)
                Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkCyan;

            writer.Serialize(item);

            Console.WriteLine();
        }
    }

    #region Print public methods

    public void Write(string text)
    {
        Write(Console.ForegroundColor, text);
    }

    public void WriteLine(string text)
    {
        WriteLine(Console.ForegroundColor, text);
    }

    public void WriteLine(ConsoleColor color, string text)
    {
        Write(color, text + Environment.NewLine);
    }

    public void Write(ConsoleColor color, string text)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
    }

    #endregion
}