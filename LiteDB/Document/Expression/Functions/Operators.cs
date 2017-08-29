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
                if (value.First.IsString || value.Second.IsString)
                {
                    yield return value.First.RawValue?.ToString() + value.Second.RawValue?.ToString();
                }
                else if (!value.First.IsNumber || !value.Second.IsNumber)
                {
                    continue;
                }
                else
                {
                    yield return value.First + value.Second;
                }
            }
        }

        public static IEnumerable<BsonValue> MINUS(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.First.IsNumber || !value.Second.IsNumber) continue;

                yield return value.First - value.Second;
            }
        }

        public static IEnumerable<BsonValue> MULTIPLY(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.First.IsNumber || !value.Second.IsNumber) continue;

                yield return value.First * value.Second;
            }
        }

        public static IEnumerable<BsonValue> DIVIDE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.First.IsNumber || !value.Second.IsNumber) continue;

                yield return value.First / value.Second;
            }
        }

        public static IEnumerable<BsonValue> MOD(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                if (!value.First.IsNumber || !value.Second.IsNumber) continue;

                yield return value.First % value.Second;
            }
        }

        public static IEnumerable<BsonValue> EQ(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First == value.Second;
            }
        }

        public static IEnumerable<BsonValue> NEQ(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First != value.Second;
            }
        }

        public static IEnumerable<BsonValue> GT(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First > value.Second;
            }
        }

        public static IEnumerable<BsonValue> GTE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First >= value.Second;
            }
        }

        public static IEnumerable<BsonValue> LT(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First < value.Second;
            }
        }

        public static IEnumerable<BsonValue> LTE(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First <= value.Second;
            }
        }

        public static IEnumerable<BsonValue> AND(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First && value.Second;
            }
        }

        public static IEnumerable<BsonValue> OR(IEnumerable<BsonValue> left, IEnumerable<BsonValue> right)
        {
            foreach (var value in left.ZipValues(right))
            {
                yield return value.First || value.Second;
            }
        }

        public static IEnumerable<BsonValue> IIF(IEnumerable<BsonValue> condition, IEnumerable<BsonValue> ifTrue, IEnumerable<BsonValue> ifFalse)
        {
            foreach (var value in condition.ZipValues(ifTrue, ifFalse).Where(x => x.First.IsBoolean))
            {
                yield return value.First.AsBoolean ? value.Second : value.Third;
            }
        }
    }
}
