using System;
using System.Reflection;

namespace LiteDB.Shell
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var o = new OptionSet(args);

            if (o.Has("upgrade"))
            {
            }
            else if(o.Has("run"))
            {
            }
            else
            {
                ShellProgram.Start(o.Extra);
            }
        }

        /// <summary>
        /// Dynamic resolve internal (embedded) old versions of LiteDB
        /// </summary>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("LiteDB,", StringComparison.OrdinalIgnoreCase))
            {
                // get version number without dots .
                var v = args.Name.Substring(16, 5).Replace(".", "");

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