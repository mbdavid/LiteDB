using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    public class LiteShell
    {
        public Dictionary<string, ILiteCommand> Commands { get; set; }

        public LiteDatabase Database { get; set; }

        public LiteShell(LiteDatabase db)
        {
            this.Database = db;
            this.Commands = new Dictionary<string, ILiteCommand>();

            var type = typeof(ILiteCommand);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);

            foreach (var t in types)
            {
                var cmd = (ILiteCommand)Activator.CreateInstance(t);
                this.Commands.Add(t.Name, cmd);
            }
        }

        public BsonValue Run(string command)
        {
            if (string.IsNullOrEmpty(command)) return BsonValue.Null;

            var s = new StringScanner(command);

            foreach (var cmd in this.Commands)
            {
                if (cmd.Value.IsCommand(s))
                {
                    if (this.Database == null)
                    {
                        throw LiteException.NoDatabase(); 
                    }

                    return cmd.Value.Execute(this.Database, s);
                }
            }

            throw LiteException.InvalidCommand(command);
        }
    }
}
