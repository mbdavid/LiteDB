using System;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Database",
        Name = "shrink",
        Syntax = "shrink <password>",
        Description = "Shrink datafile to reduce file size. Can be define a password. If password was not passed, datafile will remove password. Returns how many bytes as reduced.",
        Examples = new string[] {
            "open mydb.db",
            "open filename=mydb.db; password=johndoe; initial=100Mb"
        }
    )]
    internal class Shrink : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"db.shrink\s*").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            var password = s.Scan(".*").TrimToNull();

            env.Display.WriteLine("Shrinking datafile...");

            env.Display.WriteLine(password == null ?
                "No encryption" :
                "Encrypting datafile using password: " + password);

            env.Display.WriteLine("Reduced: " + env.Engine.Shrink(password) + " bytes");
        }
    }
}