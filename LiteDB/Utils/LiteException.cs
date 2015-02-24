using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// The main exception for LiteDB
    /// </summary>
    public class LiteException : Exception
    {
        public int ErrorCode { get; private set; }

        public LiteException(string message)
            : base(message)
        {
        }

        private LiteException(int code, string message, params object[] args)
            : base(string.Format(message, args))
        {
            this.ErrorCode = code;
        }

        public static LiteException IndexTypeNotSupport(Type type)
        {
            return new LiteException(202, "The '{0}' datatype is not valid for index", type.Name);
        }

        public static LiteException IndexKeyTooLong()
        {
            return new LiteException(202, "Index key must be less than {0} bytes", IndexService.MAX_INDEX_LENGTH);
        }
    }
}
