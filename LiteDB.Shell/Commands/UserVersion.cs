using System;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Database",
        Name = "userversion",
        Syntax = "db.userversion [<new_version>]",
        Description = "Get/Set datafile user version.",
        Examples = new string[] {
            "db.userversion",
            "db.userversion 5"
        }
    )]
    internal class UserVersion : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"db.userversion\s*").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            var ver = s.Scan(@"\d*");

            if (ver.Length > 0)
            {
                env.Engine.UserVersion = Convert.ToUInt16(ver);
            }
            else
            {
                env.Display.WriteLine(env.Engine.UserVersion.ToString());
            }
        }
    }
}