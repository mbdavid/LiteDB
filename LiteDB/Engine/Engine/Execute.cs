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
        /// Execute single SQL-Like command and return data reader (can contains single or multiple values)
        /// </summary>
        public BsonDataReader Execute(string command, BsonDocument parameters = null)
        {
            var s = new SqlParser(this, new Tokenizer(command), parameters);

            return s.Execute();
        }

        /// <summary>
        /// Execute single SQL-Like command and return data reader (can contains single or multiple values)
        /// </summary>
        public BsonDataReader Execute(string command, params BsonValue[] args)
        {
            var parameters = new BsonDocument();

            for(var i = 0; i < args.Length; i++)
            {
                parameters[i.ToString()] = args[i];
            }

            return this.Execute(command, parameters);
        }
    }
}