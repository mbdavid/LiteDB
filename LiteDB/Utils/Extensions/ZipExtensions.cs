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
        public static IEnumerable<ZipValues> ZipValues(this IEnumerable<BsonValue> first, IEnumerable<BsonValue> second, IEnumerable<BsonValue> thrid = null)
        {
            var firstEnumerator = first.GetEnumerator();
            var secondEnumerator = second.GetEnumerator();
            var thridEnumerator = thrid?.GetEnumerator();

            var firstCurrent = BsonValue.Null;
            var secondCurrent = BsonValue.Null;
            var thridCurrent = BsonValue.Null;

            // loop for read all first enumerable
            while (firstEnumerator.MoveNext())
            {
                firstCurrent = firstEnumerator.Current;

                if (secondEnumerator.MoveNext())
                {
                    secondCurrent = secondEnumerator.Current;
                }
                if (thrid != null && thridEnumerator.MoveNext())
                {
                    thridCurrent = thridEnumerator.Current;
                }

                yield return new ZipValues(firstCurrent, secondCurrent, thridCurrent);
            }

            // loop for use all second enumerable
            while (secondEnumerator.MoveNext())
            {
                secondCurrent = secondEnumerator.Current;

                if (thrid != null && thridEnumerator.MoveNext())
                {
                    thridCurrent = thridEnumerator.Current;
                }

                yield return new ZipValues(firstCurrent, secondCurrent, thridCurrent);
            }

            // loop for use all thrid enumerable (if exists)
            while (thrid != null && thridEnumerator.MoveNext())
            {
                thridCurrent = thridEnumerator.Current;

                yield return new ZipValues(firstCurrent, secondCurrent, thridCurrent);
            }
        }
    }

    internal class ZipValues
    {
        public BsonValue First { get; set; }
        public BsonValue Second { get; set; }
        public BsonValue Third { get; set; }

        public ZipValues(BsonValue first, BsonValue second, BsonValue thrid)
        {
            this.First = first;
            this.Second = second;
            this.Third = thrid;
        }
    }
}
