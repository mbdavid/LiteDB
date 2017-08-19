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
        private static IEnumerable<KeyValuePair<BsonValue, BsonValue>> Zip(IEnumerable<BsonValue> first, IEnumerable<BsonValue> second)
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

                yield return new KeyValuePair<BsonValue, BsonValue>(firstCurrent, secondCurrent);
            }
            while (secondEnumerator.MoveNext())
            {
                secondCurrent = secondEnumerator.Current;

                yield return new KeyValuePair<BsonValue, BsonValue>(firstCurrent, secondCurrent);
            }
        }
    }
}
