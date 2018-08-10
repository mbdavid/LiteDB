using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class BsonExpression
    {
        /// <summary>
        /// Get all KEYS names from a document. Support multiple values (document only)
        /// </summary>
        public static IEnumerable<BsonValue> KEYS(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDocument))
            {
                foreach(var key in value.AsDocument.Keys)
                {
                    yield return key;
                }
            }
        }

        /// <summary>
        /// Conditional IF statment. If condition are true, returns TRUE value, otherwise, FALSE value. Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> IIF(IEnumerable<BsonValue> condition, IEnumerable<BsonValue> ifTrue, IEnumerable<BsonValue> ifFalse)
        {
            foreach (var value in condition.ZipValues(ifTrue, ifFalse).Where(x => x.First.IsBoolean))
            {
                yield return value.First.AsBoolean ? value.Second : value.Third;
            }
        }

        /// <summary>
        /// Return length of variant value (valid only for String, Binary, Array or Document [keys])
        /// </summary>
        public static IEnumerable<BsonValue> LENGTH(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsString) yield return value.AsString.Length;
                else if (value.IsBinary) yield return value.AsBinary.Length;
                else if (value.IsArray) yield return value.AsArray.Count;
                else if (value.IsDocument) yield return value.AsDocument.Keys.Count;
            }
        }
    }
}
