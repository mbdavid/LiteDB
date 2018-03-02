using LiteDB.Shell;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LiteDB
{
    public partial class LiteEngine
    {
        private static List<ICommand> _commands = new List<ICommand>();

        /// <summary>
        /// Run a shell command from a string. Execute command in current database and returns an IList collection of results
        /// </summary>
        public IList<BsonValue> Run(string command, BsonDocument parameters, LiteTransaction transaction)
        {
            if (_commands.Count == 0)
            {
                RegisterCommands();
            }

            var s = new StringScanner(command);

            // test all commands
            foreach (var cmd in _commands)
            {
                if (!cmd.IsCommand(s)) continue;

                var values = cmd.Execute(s, this, transaction, parameters);

                return values.ToList();
            }

            throw LiteException.InvalidCommand(command);
        }

        #region Register all shell commands

        private static void RegisterCommands()
        {
            lock (_commands)
            {
                var type = typeof(ICommand);
                var types = typeof(LiteEngine)
                    .GetTypeInfo().Assembly
                    .GetTypes()
                    .Where(p => type.IsAssignableFrom(p) && p.GetTypeInfo().IsClass);

                _commands.Clear();

                foreach (var cmd in types)
                {
                    _commands.Add(Activator.CreateInstance(cmd) as ICommand);
                }
            }
        }

        #endregion
    }
}