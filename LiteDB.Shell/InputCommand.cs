using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    public class InputCommand
    {
        private Queue<string> _queue;
        private string _last = "";

        public InputCommand()
        {
            _queue = new Queue<string>();
        }

        public string ReadCommand()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("> ");

            var cmd = this.ReadLine();

            // suport for multiline command
            if (cmd.StartsWith("/"))
            {
                cmd = cmd.Substring(1);

                while (!cmd.EndsWith("/"))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("| ");

                    var line = this.ReadLine();
                    cmd += line;
                }

                cmd = cmd.Substring(0, cmd.Length - 1);
            }

            cmd = cmd.Trim();

            if (cmd == "ed")
            {
                this.OpenNotepad();
                return null;
            }
            else if (cmd.StartsWith("run "))
            {
                this.RunCommand(cmd.Substring(4));
                return null;
            }

            _last = cmd;

            return cmd.Trim();
        }

        /// <summary>
        /// Read a line from queue or user
        /// </summary>
        private string ReadLine()
        {
            Console.ForegroundColor = ConsoleColor.Gray;

            if (_queue.Count > 0)
            {
                var cmd = _queue.Dequeue();
                Console.WriteLine(cmd);
                return cmd;
            }
            else
            {
                return Console.ReadLine();
            }
        }

        /// <summary>
        /// Open notepad and add each line as a new command
        /// </summary>
        private void OpenNotepad()
        {
            var temp = Path.GetTempPath() + "LiteDB.Shell.txt";

            File.WriteAllText(temp, _last.Replace("\n", Environment.NewLine));

            Process.Start("notepad.exe", temp).WaitForExit();

            foreach (var line in File.ReadAllLines(temp))
            {
                _queue.Enqueue(line);
            }
        }

        /// <summary>
        /// Open a file and get each line as a new command
        /// </summary>
        private void RunCommand(string filename)
        {
            foreach (var line in File.ReadAllLines(filename.Trim()))
            {
                _queue.Enqueue(line);
            }
        }
    }
}
