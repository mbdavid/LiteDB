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

        public static IEnumerable<BsonValue> MIN(IEnumerable<BsonValue> values)
        {
            var min = BsonValue.MaxValue;

            foreach(var value in values.Where(x => x.IsNumber))
            {
                min = value < min ? value : min;
            }

            yield return min == BsonValue.MaxValue ? BsonValue.MinValue : min;
        }

        public static IEnumerable<BsonValue> MAX(IEnumerable<BsonValue> values)
        {
            var max = BsonValue.MinValue;

            foreach (var value in values.Where(x => x.IsNumber))
            {
                max = value > max ? value : max;
            }

            yield return max == BsonValue.MinValue ? BsonValue.MaxValue : max;
        }

        public static IEnumerable<BsonValue> AVG(IEnumerable<BsonValue> values)
        {
            var sum = new BsonValue(0);
            var count = 0;

            foreach (var value in values.Where(x => x.IsNumber))
            {
                sum += value;
                count++;
            }

            if (count > 0)
            {
                yield return sum / count;
            }
        }

        public static IEnumerable<BsonValue> SUM(IEnumerable<BsonValue> values)
        {
            var sum = new BsonValue(0);

            foreach (var value in values.Where(x => x.IsNumber))
            {
                sum += value;
            }

            yield return sum;
        }
    }
}
