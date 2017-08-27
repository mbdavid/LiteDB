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
        public static IEnumerable<BsonValue> ARRAY(IEnumerable<BsonValue> values)
        {
            yield return new BsonArray(values);
        }

        public static IEnumerable<BsonValue> IS_DATE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDateTime;
            }
        }

        public static IEnumerable<BsonValue> IS_NUMBER(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsNumber;
            }
        }

        public static IEnumerable<BsonValue> IS_STRING(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsString;
            }
        }
    }
}
