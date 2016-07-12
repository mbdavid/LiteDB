using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LiteDB.Interfaces;
#if PCL
using System.Reflection;
#endif

namespace LiteDB.Shell
{
    public class LiteShell
    {
        private static List<IShellCommand> _commands = new List<IShellCommand>();

        static LiteShell()
        {
            var type = typeof(IShellCommand);
#if NETFULL
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass);
#elif PCL
           var shellCommandTypeInfo = type.GetTypeInfo();

           var assembly = typeof(LiteShell).GetTypeInfo().Assembly;

         var types = assembly.ExportedTypes
               .Where(p =>
               {
                  var typeInfo = p.GetTypeInfo();

                  if (!typeInfo.IsClass)
                     return false;

                  return shellCommandTypeInfo.IsAssignableFrom(typeInfo);
               });
#endif

            foreach (var t in types)
            {
                var cmd = (IShellCommand)Activator.CreateInstance(t);
                _commands.Add(cmd);
            }
        }

       private DbEngine m_engine;

       public LiteShell(ILiteDatabase database)
       {
          m_engine = database.Engine;
       }

      /// <summary>
      /// Run a command shell
      /// </summary>
      public BsonValue Run(string command)
      {
         return Run(m_engine, command);
      }


      /// <summary>
      /// Run a command shell formating $0, $1 to JSON string args item index
      /// </summary>
      public BsonValue Run(string command, params BsonValue[] args)
      {
         var cmd = Regex.Replace(command, @"\$(\d+)", (k) =>
         {
            var index = Convert.ToInt32(k.Groups[1].Value);
            return JsonSerializer.Serialize(args[index]);
         });

         return Run(m_engine, cmd);
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