using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal static class ZipExtensions
    {
        public static IEnumerable<ZipValues> ZipValues(this IEnumerable<BsonValue> first, IEnumerable<BsonValue> second)
        {
            var firstEnumerator = first.GetEnumerator();
            var secondEnumerator = second.GetEnumerator();
            var firstCurrent = BsonValue.Null;
            var secondCurrent = BsonValue.Null;

            while (firstEnumerator.MoveNext())
            {
                firstCurrent = firstEnumerator.Current;

                if (secondEnumerator.MoveNext())
                {
                    secondCurrent = secondEnumerator.Current;
                }

                yield return new ZipValues(firstCurrent, secondCurrent);
            }
            while (secondEnumerator.MoveNext())
            {
                secondCurrent = secondEnumerator.Current;

                yield return new ZipValues(firstCurrent, secondCurrent);
            }
        }

    }

    internal class ZipValues
    {
        public BsonValue Left { get; set; }
        public BsonValue Right { get; set; }

        public ZipValues(BsonValue left, BsonValue right)
        {
            this.Left = left;
            this.Right = right;
        }
    }
}
