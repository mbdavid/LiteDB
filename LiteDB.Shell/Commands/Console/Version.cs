using System;

namespace LiteDB.Shell.Commands
{
    internal class Version : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"ver(sion)?$").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var assembly = typeof(LiteDatabase).Assembly.GetName();

            display.WriteLine(assembly.FullName);
        }
    }
}