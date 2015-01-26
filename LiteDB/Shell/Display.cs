using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    public class Display
    {
        public List<TextWriter> TextWriters { get; set; }
        public bool Pretty { get; set; }

        public Display()
        {
            this.TextWriters = new List<TextWriter>();
        }

        public void WriteWelcome()
        {
            this.WriteInfo("Welcome to LiteDB Shell");
            this.WriteInfo("");
            this.WriteInfo("Getting started with `help`");
            this.WriteInfo("");
        }

        public void WritePrompt(string text)
        {
            this.Write(ConsoleColor.White, text);
        }

        public void WriteResult(string text)
        {
            this.WriteLine(ConsoleColor.DarkCyan, text);
        }

        public void WriteInfo(string text)
        {
            this.WriteLine(ConsoleColor.Gray, text);
        }

        public void WriteError(string err)
        {
            this.WriteLine(ConsoleColor.Red, err);
        }

        public void WriteHelp(string line1, string line2)
        {
            this.WriteLine(ConsoleColor.Cyan, line1);
            this.WriteLine(ConsoleColor.DarkCyan, "    " + line2);
        }

        public void WriteBson(BsonValue result)
        {
            this.WriteLine(ConsoleColor.DarkCyan, JsonEx.Serialize(result, this.Pretty, false));
        }

        public void WriteBson<T>(IEnumerable<T> result)
            where T : BsonValue
        {
            var index = 0;

            foreach (var doc in result)
            {
                this.Write(ConsoleColor.Cyan, string.Format("[{0}]:{1}", ++index, this.Pretty ? Environment.NewLine : " "));
                this.WriteBson(doc);
            }

            if (index == 0)
            {
                this.WriteLine(ConsoleColor.DarkCyan, "no documents");
            }
        }

        #region Private methods

        private void Write(string text)
        {
            this.Write(Console.ForegroundColor, text);
        }

        private void WriteLine(string text)
        {
            this.WriteLine(Console.ForegroundColor, text);
        }

        private void WriteLine(ConsoleColor color, string text)
        {
            this.Write(color, text + Environment.NewLine);
        }

        private void Write(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;

            foreach (var writer in this.TextWriters)
            {
                writer.Write(text);
            }
        }

        #endregion
    }
}
