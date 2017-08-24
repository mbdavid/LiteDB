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
        private static IEnumerable<BinaryValues> Zip(IEnumerable<BsonValue> first, IEnumerable<BsonValue> second)
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

                yield return new BinaryValues(firstCurrent, secondCurrent);
            }
            while (secondEnumerator.MoveNext())
            {
                secondCurrent = secondEnumerator.Current;

                yield return new BinaryValues(firstCurrent, secondCurrent);
            }
        }

    }

    internal class BinaryValues
    {
        public BsonValue Left { get; set; }
        public BsonValue Right { get; set; }

        public BinaryValues(BsonValue left, BsonValue right)
        {
            this.Left = left;
            this.Right = right;
        }
    }
}
