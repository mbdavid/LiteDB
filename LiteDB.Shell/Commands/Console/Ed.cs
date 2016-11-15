using System;
using System.Diagnostics;
using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Ed : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"ed$");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
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