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

            // remove "ed" command from history
            input.History.RemoveAt(input.History.Count - 1);

            var last = input.History.Count > 0 ? input.History[input.History.Count - 1] : "";

            File.WriteAllText(temp, last.Replace("\n", Environment.NewLine));

            Process.Start("notepad.exe", temp).WaitForExit();

            var text = File.ReadAllText(temp);

            if (text == last) return;

            input.Queue.Enqueue(text);
        }
    }
}
