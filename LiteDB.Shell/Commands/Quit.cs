using System;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Shell",
        Name = "quit",
        Syntax = "quit|exit",
        Description = "Close shell application"
    )]
    internal class Quit : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"(quit|exit)$");
        }

        public void Execute(StringScanner s, Env env)
        {
            env.Close();
            env.Input.Running = false;
        }
    }
}