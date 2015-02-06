using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Ed : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Match(@"ed$");
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
            var temp = Path.GetTempPath() + "LiteDB.Shell.txt";

            File.WriteAllText(temp, input.Last.Replace("\n", Environment.NewLine));

            Process.Start("notepad.exe", temp).WaitForExit();

            var text = File.ReadAllText(temp);

            if (text == input.Last) return;

            foreach (var line in text.Split('\n'))
            {
                input.Queue.Enqueue(line);
            }
        }
    }
}
