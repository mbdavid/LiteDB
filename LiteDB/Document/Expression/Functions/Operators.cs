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
                // if any side are string, concat
                if (value.Left.IsString || value.Right.IsString)
                {
                    yield return value.Left.RawValue?.ToString() + value.Right.RawValue?.ToString();
                }
                else if (!value.Left.IsNumber || !value.Right.IsNumber)
                {
                    continue;
                }
                else
                {
                    yield return value.Left + value.Right;
                }
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

                yield return value.Left / value.Right;
            }
        }

        public static IEnumerable<BsonValue> EQ(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left == value.Right;
            }
        }

        public static IEnumerable<BsonValue> NEQ(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left != value.Right;
            }
        }

        public static IEnumerable<BsonValue> GT(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left > value.Right;
            }
        }

        public static IEnumerable<BsonValue> GTE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left >= value.Right;
            }
        }

        public static IEnumerable<BsonValue> LT(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left < value.Right;
            }
        }

        public static IEnumerable<BsonValue> LTE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left <= value.Right;
            }
        }

        public static IEnumerable<BsonValue> AND(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left && value.Right;
            }
        }

        public static IEnumerable<BsonValue> OR(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.Left || value.Right;
            }
        }
    }
}
