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
        public static IEnumerable<BsonValue> ADD(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.Left.IsNumber || !value.Right.IsNumber) continue;

                yield return value.Left + value.Right;
            }
        }

        public static IEnumerable<BsonValue> MINUS(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.Left.IsNumber || !value.Right.IsNumber) continue;

                yield return value.Left - value.Right;
            }
        }

        public static IEnumerable<BsonValue> MULTIPLY(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.Left.IsNumber || !value.Right.IsNumber) continue;

                yield return value.Left * value.Right;
            }
        }

        public static IEnumerable<BsonValue> DIVIDE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.Left.IsNumber || !value.Right.IsNumber) continue;

                yield return value.Left  / value.Right;
            }
        }
    }
}
