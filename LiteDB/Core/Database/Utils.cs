using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteDatabase : IDisposable
    {
        /// <summary>
        /// Reduce datafile size re-creating all collection in another datafile - return how many bytes are reduced.
        /// </summary>
        public int Shrink()
        {
            return _engine.Value.Shrink();
        }

        /// <summary>
        /// Convert a BsonDocument to a class object using BsonMapper rules
        /// </summary>
        public T ToObject<T>(BsonDocument doc)
            where T : new()
        {
            return _mapper.ToObject<T>(doc);
        }

        /// <summary>
        /// Convert an entity class instance into a BsonDocument using BsonMapper rules
        /// </summary>
        public BsonDocument ToDocument(object entity)
        {
            return _mapper.ToDocument(entity);
        }

        // commom shell instance
        private LiteShell _shell = null;

        /// <summary>
        /// Run a command shell
        /// </summary>
        public BsonValue Run(string command)
        {
            return this.Run(command, new BsonValue[0]);
        }

        /// <summary>
        /// Run a command shell formating {n} to JSON string args item index
        /// </summary>
        public BsonValue Run(string command, params BsonValue[] args)
        {
            if (_shell == null)
            {
                _shell = new LiteShell(this);
            }

            return _shell.Run(string.Format(command, args.Select(x => JsonSerializer.Serialize(x))));
        }

        internal string DumpPages(uint startPage = 0, uint endPage = uint.MaxValue)
        {
            return _engine.Value.DumpPages(startPage, endPage).ToString();
        }

        internal string DumpIndex(string colName, string field)
        {
            return _engine.Value.DumpIndex(colName, field).ToString();
        }
    }
}
