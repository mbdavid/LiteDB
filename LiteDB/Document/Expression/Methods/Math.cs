using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static LiteDB.ZipExtensions;

namespace LiteDB
{
    internal partial class BsonExpressionMethods
    {
        /// <summary>
        /// Apply absolute value (ABS) method in all number values
        /// </summary>
        public static IEnumerable<BsonValue> ABS(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsNumber))
            {
                switch (value.Type)
                {
                    case BsonType.Int32: yield return Math.Abs(value.AsInt32); break;
                    case BsonType.Int64: yield return Math.Abs(value.AsInt64); break;
                    case BsonType.Double: yield return Math.Abs(value.AsDouble); break;
                    case BsonType.Decimal: yield return Math.Abs(value.AsDecimal); break;
                }
            }
        }

        /// <summary>
        /// Round number method in all number values
        /// </summary>
        public static IEnumerable<BsonValue> ROUND(IEnumerable<BsonValue> values, IEnumerable<BsonValue> digits)
        {
            foreach (var value in ZipValues(values, digits))
            {
                if (!value.First.IsNumber) continue;
                if (!value.Second.IsNumber) continue;

                switch (value.First.Type)
                {
                    case BsonType.Int32: yield return value.First.AsInt32; break;
                    case BsonType.Int64: yield return value.First.AsInt64; break;
                    case BsonType.Double: yield return Math.Round(value.First.AsDouble, value.Second.AsInt32); break;
                    case BsonType.Decimal: yield return Math.Round(value.First.AsDecimal, value.Second.AsInt32); break;
                }
            }
        }

        /// <summary>
        /// Implement POWER (x and y)
        /// </summary>
        public static IEnumerable<BsonValue> POW(IEnumerable<BsonValue> x, IEnumerable<BsonValue> y)
        {
            foreach (var value in ZipValues(x, y))
            {
                if (!value.First.IsNumber) continue;
                if (!value.Second.IsNumber) continue;

                yield return Math.Pow(value.First.AsDouble, value.Second.AsDouble);
            }
        }
    }
}
