using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal class AutoId
    {
        /// <summary>
        /// Function to test if type is empty
        /// </summary>
        public Func<object, bool> IsEmpty { get; set; }

        /// <summary>
        /// Function that implements how generate a new Id for this type
        /// </summary>
        public Func<LiteCollection<BsonDocument>, object> NewId { get; set; }
    }
}
