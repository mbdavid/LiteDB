using System;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Debug : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"debug\s*").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display d, InputCommand input, Env env)
        {
            var sb = new StringBuilder();
            var enabled = !(s.Scan(@"off\s*").Length > 0);

            env.Log.Level = enabled ? Logger.FULL : Logger.NONE;
        }
    }
}