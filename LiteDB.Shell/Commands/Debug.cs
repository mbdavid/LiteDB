using System;
using System.Text;

namespace LiteDB.Shell.Commands
{
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