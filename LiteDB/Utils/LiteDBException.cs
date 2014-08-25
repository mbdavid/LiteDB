using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// The main exception for database
    /// </summary>
    public class LiteDBException : Exception
    {
        public LiteDBException(string message)
            : base(message)
        {
        }
    }
}
