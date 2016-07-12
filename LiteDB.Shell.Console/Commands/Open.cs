using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class Open : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"open\s+").Length > 0;
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            var connectionString = s.Scan(@".+");

            if (engine != null)
            {
                engine.Dispose();
            }

            // get filename, detect engine and open
            var filename = this.GetFilename(connectionString);
            engine = this.DetectEngine(filename);
            engine.Open(connectionString);

            // get engine version and display info
            var ver = engine.Version;
            display.WriteLine(ConsoleColor.DarkCyan, string.Format("open \"{0}\" (v{1}.{2}.{3})",
                Path.GetFileName(filename), ver.Major, ver.Minor, ver.Build));
        }

        public IShellEngine DetectEngine(string filename)
        {
            var engines = new IShellEngine[]
            {
                new ShellEngine_090(),
                new ShellEngine_104(),
                new ShellEngine_200()
            };

            // new files use always lastest version
            if (!File.Exists(filename))
            {
                return new ShellEngine_200();
            }

            foreach (var engine in engines)
            {
                if (engine.Detect(filename)) return engine;
            }

            throw new ShellExpcetion("Invalid dabatase format");
        }

        /// <summary>
        /// Get filename from connection string
        /// </summary>
        private string GetFilename(string connectionString)
        {
            Dictionary<string, string> values;

            // Create a dictionary from string name=value collection
            if (connectionString.Contains("="))
            {
                values = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Split(new char[] { '=' }, 2))
                    .ToDictionary(t => t[0].Trim().ToLower(), t => t.Length == 1 ? "" : t[1].Trim(), StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                // If connectionstring is only a filename, set filename
                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                values["filename"] = Path.GetFullPath(connectionString);
            }

            if(!values.ContainsKey("filename")) throw new ShellExpcetion("Invalid connection string. Missing filename");

            return values["filename"];
        }
    }
}