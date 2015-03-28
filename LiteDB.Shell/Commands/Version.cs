using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Version : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Match(@"ver(sion)?$");
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
            var ver = typeof(LiteDatabase).Assembly.GetName().Version;

            display.WriteInfo(string.Format("v{0}.{1}.{2}", 
                ver.Major,
                ver.Minor,
                ver.Build));
        }
    }
}
