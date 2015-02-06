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
        public Queue<string> Queue { get; set; }
        public string Last { get; set; }
        public Stopwatch Timer { get; set; }

        public InputCommand()
        {
            this.Queue = new Queue<string>();
            this.Last = "";
            this.Timer = new Stopwatch();
        }

        public string ReadCommand()
        {
            if (this.Timer.IsRunning)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(this.Timer.ElapsedMilliseconds.ToString("0000") + " ");
            }

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

            this.Last = cmd;

            if (this.Timer.IsRunning)
            {
                this.Timer.Reset();
                this.Timer.Start();
            }

            return cmd.Trim();
        }

        /// <summary>
        /// Read a line from queue or user
        /// </summary>
        private string ReadLine()
        {
            Console.ForegroundColor = ConsoleColor.Gray;

            if (this.Queue.Count > 0)
            {
                var cmd = this.Queue.Dequeue();
                Console.WriteLine(cmd);
                return cmd;
            }
            else
            {
                return Console.ReadLine();
            }
        }
    }
}
