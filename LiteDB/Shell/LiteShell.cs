using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LiteDB.Shell
{
    internal class LiteShell
    {
        private static List<IShellCommand> _commands = new List<IShellCommand>();

        static LiteShell()
        {
            var type = typeof(IShellCommand);
#if NETFULL
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);
#elif NETCORE
            var types = typeof(LiteShell).GetTypeInfo().Assembly.GetTypes()
               .Where(p => type.IsAssignableFrom(p) && p.GetTypeInfo().IsClass);
#else
            // PCL Reflection on PCL only allows public types, so there's no point
            // since all shell types are internal
            IEnumerable<Type> types = new List<Type>();
#endif

            foreach (var t in types)
            {
                var cmd = (IShellCommand)Activator.CreateInstance(t);
                _commands.Add(cmd);
            }
        }

        public BsonValue Run(DbEngine engine, string command)
        {
            if (string.IsNullOrEmpty(command)) return BsonValue.Null;

            var s = new StringScanner(command);

            foreach (var cmd in _commands)
            {
                if (cmd.IsCommand(s))
                {
                    return cmd.Execute(engine, s);
                }
            }

            throw LiteException.InvalidCommand(command);
        }
    }
}