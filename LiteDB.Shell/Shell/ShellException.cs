using System;

namespace LiteDB.Shell
{
    internal class ShellExpcetion : Exception
    {
        public ShellExpcetion(string message)
            : base(message)
        {
        }

        public static ShellExpcetion NoDatabase()
        {
            return new ShellExpcetion("No open database");
        }
    }
}