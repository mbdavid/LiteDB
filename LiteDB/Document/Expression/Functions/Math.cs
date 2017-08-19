using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal partial class LiteExpression
    {
        public static IEnumerable<BsonValue> ADD(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in Zip(left, right))
            {
                if (!value.Key.IsNumber || !value.Value.IsNumber) continue;

                yield return value.Key + value.Value;
            }
        }

        public static IEnumerable<BsonValue> MINUS(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in Zip(left, right))
            {
                if (!value.Key.IsNumber || !value.Value.IsNumber) continue;

                yield return value.Key + value.Value;
            }
        }

        public static IEnumerable<BsonValue> MULTIPLY(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in Zip(left, right))
            {
                if (!value.Key.IsNumber || !value.Value.IsNumber) continue;

                yield return value.Key * value.Value;
            }
        }

        public static IEnumerable<BsonValue> DIVIDE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in Zip(left, right))
            {
                if (!value.Key.IsNumber || !value.Value.IsNumber) continue;

                yield return value.Key / value.Value;
            }
        }
    }
}
