using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    [Help(
        Category = "Database",
        Name = "export",
        Syntax = "db.<collection>.export <filename>",
        Description = "Export collection as JSON file",
        Examples = new string[] {
            "db.customers.export C:\\Temp\\customers.json"
        }
    )]
    internal class Export : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"db.[\w$]*.export\s*");
        }

        public void Execute(StringScanner s, Env env)
        {
            var colname = s.Scan(@"db.([\w$]*).export\s*", 1);
            var filename = s.Scan(".*").TrimToNull();

            var counter = env.Engine.Count(colname);
            var index = 0;

            using (var fs = new FileStream(filename, System.IO.FileMode.CreateNew))
            {
                using (var writer = new StreamWriter(fs))
                {
                    writer.WriteLine("[");

                    foreach (var doc in env.Engine.FindAll(colname))
                    {
                        var json = JsonSerializer.Serialize(doc, false, true);

                        index++;

                        writer.Write(json);

                        if (index < counter) writer.Write(",");

                        writer.WriteLine();
                    }

                    writer.Write("]");
                    writer.Flush();
                }
            }

            env.Display.WriteLine($"File {filename} created with {counter} documents");
        }
    }
}