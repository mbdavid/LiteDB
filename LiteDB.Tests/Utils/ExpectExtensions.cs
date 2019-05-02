using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public static class ExpectExtensions
    {
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