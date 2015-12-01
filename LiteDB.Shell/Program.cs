using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    class Program
    {
        static void Main(string[] args)
        {
            var o = new OptionSet(args);

            if(o.Upgrade != null)
            {
                // do upgrade
            }
            else if(o.Run != null)
            {
            }
            else
            {
                ShellProgram.Start(o.Filename);
            }
        }
    }
}
