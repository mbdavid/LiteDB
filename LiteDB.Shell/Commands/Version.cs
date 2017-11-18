using System;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Shell",
        Name = "version",
        Syntax = "ver",
        Description = "Show LiteDB version"
    )]
    internal class Version : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"ver(sion)?$").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            var assembly = typeof(LiteDatabase).Assembly.GetName();

            env.Display.WriteLine(assembly.FullName);
        }
    }
}