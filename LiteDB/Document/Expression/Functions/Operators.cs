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
        public static IEnumerable<BsonValue> EQ(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left == value.Right;
            }
        }

        public static IEnumerable<BsonValue> FILTER(IEnumerable<BsonValue> values, IEnumerable<BsonValue> conditional)
        {
            foreach (var value in values.ZipValues(conditional))
            {
                if(value.Right.IsBoolean && value.Right.AsBoolean)
                {
                    yield return value.Left;
                }
            }
        }
    }
}
