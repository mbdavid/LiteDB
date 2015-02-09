using LiteDB.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteDatabase
    {
        private LiteShell _shell = null;

        /// <summary>
        /// Run a shell command in current database. Returns a BsonValue as result
        /// </summary>
        public BsonValue RunCommand(string command)
        {
            if (_shell == null)
            {
                _shell = new LiteShell();
                _shell.RegisterAll();
                _shell.Database = this;
            }
            return _shell.Run(command);
        }
    }
}
