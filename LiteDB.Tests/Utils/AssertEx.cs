using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    /// <summary>
    /// Extension and addicional methods for debug
    /// </summary>
    public static class AssertEx
    {
        [DebuggerHidden]
        public static void ArrayEqual<T>(T[] first, T[] second, bool sort)
            where T : IEqualityComparer<T>
        {
            if (first.Length != second.Length)
            {
                Assert.Fail("Arrays with diferent item count");
            }

            if (sort)
            {
                first = first.OrderBy(x => x).ToArray();
                second = second.OrderBy(x => x).ToArray();
            }

            var index = 0;

            foreach (var zip in first.Zip(second, (First, Second) => new { First, Second }))
            {
                var r = zip.First.Equals(zip.First, zip.Second);

                if (r == false)
                {
                    Assert.Fail($"Index [{index}]: values are not same: `{zip.First}` != `{zip.Second}`");
                }

                index++;
            }
        }

        [DebuggerHidden]
        public static void ExpectValue(this BsonValue value, BsonValue expect)
        {
            Assert.AreEqual(expect, value);
        }

        [DebuggerHidden]
        public static void ExpectValue<T>(this T value, T expect)
        {
            Assert.AreEqual(expect, value);
        }

        [DebuggerHidden]
        public static void ExpectArray(this BsonValue value, params BsonValue[] args)
        {
            Assert.AreEqual(new BsonArray(args), value);
        }

        [DebuggerHidden]
        public static void ExpectJson(this BsonValue value, string expectJson)
        {
            Assert.AreEqual(JsonSerializer.Deserialize(expectJson), value);
        }

        [DebuggerHidden]
        public static void ExpectValues(this IEnumerable<BsonValue> values, params BsonValue[] expectValues)
        {
            CollectionAssert.AreEqual(expectValues, values.ToArray());
        }

        [DebuggerHidden]
        public static void ExpectValues<T>(this IEnumerable<T> values, params T[] expectValues)
        {
            CollectionAssert.AreEqual(expectValues, values.ToArray());
        }

        [DebuggerHidden]
        public static void ExpectCount<T>(this IEnumerable<T> values, int count)
        {
            Assert.AreEqual(count, values.Count());
        }
    }
}
