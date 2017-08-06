using System;

namespace LiteDB.Shell.Commands
{
    internal class Timer : ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"timer$");
        }

        public void Execute(StringScanner s, Env env)
        {
            env.Input.Timer.Start();
        }
    }
}