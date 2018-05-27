using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Run shell command returing a single value result
        /// </summary>
        public void Run(string command, BsonDocument parameters, IShellOutput result)
        {
            var s = new ShellParser(this, new Tokenizer(command), parameters, result);

            s.Execute();
        }
    }
}