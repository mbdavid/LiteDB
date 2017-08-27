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
        public static IEnumerable<BsonValue> COUNT(IEnumerable<BsonValue> values)
        {
            yield return values.Count();
        }

        public static IEnumerable<BsonValue> SUM(IEnumerable<BsonValue> values)
        {
            var sum = new BsonValue(0);

            foreach (var value in values)
            {
                if (!value.IsNumber) continue;

                sum += value;
            }

            yield return sum;
        }
    }
}
