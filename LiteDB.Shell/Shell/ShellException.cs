using System;

namespace LiteDB.Shell
{
    internal class ShellException : Exception
    {
        public ShellException(string message)
            : base(message)
        {
        }

        public static ShellException NoDatabase()
        {
            return new ShellException("No open database");
        }
    }
}