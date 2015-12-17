using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            ShellProgram.Start(args.Length > 0 ? args[0] : null);
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