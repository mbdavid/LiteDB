using System;

namespace LiteDB.Shell.Commands
{
    internal class Comment : ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"--");
        }

        public void Execute(StringScanner s, Env env)
        {
        }
    }
}