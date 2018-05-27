using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    public class Display : IShellOutput
    {
        public List<TextWriter> TextWriters { get; set; }
        public bool Pretty { get; set; }

        public int Limit { get; set; }

        public Display()
        {
            this.TextWriters = new List<TextWriter>();
            this.Pretty = false;
            this.Limit = 1000;
        }

        public void Write(BsonValue value, int index, int resultset)
        {
            if (index >= 0)
            {
                this.Write(ConsoleColor.Cyan, string.Format("[{0}]: ", index));
            }

            if (value.IsNumber || value.IsString)
            {
                this.WriteLine(ConsoleColor.DarkCyan, value.RawValue.ToString());
            }
            else
            {
                this.WriteLine(ConsoleColor.DarkCyan, JsonSerializer.Serialize(value, this.Pretty, false));
            }
        }

        public void Write(Exception ex)
        {
            this.WriteLine(ConsoleColor.Red, ex.Message);
        }

        public void WriteWelcome()
        {
            this.WriteLine("Welcome to LiteDB Shell");
            this.WriteLine("");
            this.WriteLine("Getting started with `help`, `help full` or `help <command>`");
            this.WriteLine("");
        }

        public void WritePrompt(string text)
        {
            this.Write(ConsoleColor.White, text);
        }

        #region Print public methods

        public void Write(string text)
        {
            this.Write(Console.ForegroundColor, text);
        }

        public void WriteLine(string text)
        {
            this.WriteLine(Console.ForegroundColor, text);
        }

        public void WriteLine(ConsoleColor color, string text)
        {
            this.Write(color, text + Environment.NewLine);
        }

        public void Write(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;

            foreach (var writer in this.TextWriters)
            {
                writer.Write(Regex.Unescape(text));
            }
        }

        #endregion
    }
}