using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    internal class Program
    {
        /// <summary>
        /// Opens console shell app. Usage:
        /// LiteDB.Shell [myfile.db] --param1 value1 --params2 "value 2"
        /// Parameters:
        /// --exec "command"   : Execute an shell command (can be multiples --exec)
        /// --run script.txt   : Run script commands file 
        /// --pretty           : Show JSON in multiline + idented
        /// --upgrade newdb.db : Upgrade database to lastest version
        /// --exit             : Exit after last command
        /// </summary>
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var input = new InputCommand();
            var display = new Display();
            var o = new OptionSet();

            // default arg
            o.Register((v) => input.Queue.Enqueue("open " + v));
            o.Register("pretty", () => display.Pretty = true);
            o.Register("exit", () => input.AutoExit = true);
            o.Register<string>("run", (v) => input.Queue.Enqueue("run " + v));
            o.Register<string>("exec", (v) => input.Queue.Enqueue(v));
            o.Register<string>("upgrade", (v) =>
            {
                var tmp = Path.GetTempFileName();
                input.Queue.Enqueue("dump > " + tmp);
                input.Queue.Enqueue("open " + v);
                input.Queue.Enqueue("dump < " + tmp);
            });

            // parse command line calling register parameters
            o.Parse(args);

            ShellProgram.Start(input, display);
        }

        /// <summary>
        /// Dynamic resolve internal (embedded) old versions of LiteDB
        /// </summary>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var match = Regex.Match(args.Name, @"^LiteDB, Version=(\d+)\.(\d+).(\d+)");

            if (match.Success)
            {
                // get version number without dots .
                var v = match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value;

                // load assembly from resource stream  manifest
                var stream = Assembly.GetEntryAssembly().GetManifestResourceStream("LiteDB.Shell.Resources.LiteDB" + v + ".dll");
                var buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int)stream.Length);
                return Assembly.Load(buffer);
            }

            throw new ArgumentNullException();
        }
    }
}