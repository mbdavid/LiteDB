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
    }
}
