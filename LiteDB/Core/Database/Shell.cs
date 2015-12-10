using LiteDB.Shell;
using System;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        /// <summary>
        /// Run a command shell
        /// </summary>
        public BsonValue Run(string command)
        {
            var shell = new LiteShell();

            return shell.Run(_engine.Value, command);
        }

        /// <summary>
        /// Run a command shell formating $0, $1 to JSON string args item index
        /// </summary>
        public BsonValue Run(string command, params BsonValue[] args)
        {
            var shell = new LiteShell();

            var cmd = Regex.Replace(command, @"\$(\d+)", (k) =>
            {
                var index = Convert.ToInt32(k.Groups[1].Value);
                return JsonSerializer.Serialize(args[index]);
            });

            return shell.Run(_engine.Value, cmd);
        }
    }
}