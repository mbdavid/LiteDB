using System;
using System.Text;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Shell",
        Name = "debug",
        Syntax = "debug [on|off|<level>]",
        Description = "Enabled debug messages from database engine and write on console. Level can be defined as byte value: ERROR = 1; RECOVERY = 2; COMMAND = 4; LOCK = 8; QUERY = 16; JOURNAL = 32; CACHE = 64; DISK = 128; FULL = 255",
        Examples = new string[] {
            "debug 16",
            "debug on"
        }
    )]
    internal class Debug : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"debug\s*").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            var sb = new StringBuilder();
            var enabled = s.Scan(@"(on|off|\d+)*").ToLower();

            env.Log.Level = 
                enabled == "" || enabled == "on" ? Logger.FULL :
                enabled == "off" ? Logger.NONE :
                Convert.ToByte(enabled);
        }
    }
}