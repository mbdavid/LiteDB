using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public static class Sql
    {
        private const string MESSAGE = "This method are used only in LINQ Expressions to be converted into BsonExpression";

        public static T Items<T>(this IEnumerable<T> items) => throw new NotSupportedException(MESSAGE);

        public static T Items<T>(this IEnumerable<T> items, int index) => throw new NotSupportedException(MESSAGE);

        public static T Items<T>(this IEnumerable<T> items, Expression<Func<T, bool>> predicate) => throw new NotSupportedException(MESSAGE);

        public static int Count<T>(T values) => throw new NotSupportedException(MESSAGE);

        public static T Min<T>(T values) => throw new NotSupportedException(MESSAGE);

        public static T Max<T>(T values) => throw new NotSupportedException(MESSAGE);

        public static T First<T>(T values) => throw new NotSupportedException(MESSAGE);

        public static T Last<T>(T values) => throw new NotSupportedException(MESSAGE);

        public static T[] ToArray<T>(T values) => throw new NotSupportedException(MESSAGE);

        public static List<T> ToList<T>(T values) => throw new NotSupportedException(MESSAGE);

        /// <summary>
        /// Get difference of 2 dates. Can define type of date internval. Use dateInterval: "y" (or "year"), "M" (or "month"), "d" (or "day"), "h" (or "hour"), "m" (or "minute"), "s" or ("second").
        /// </summary>
        public static int DateDiff(string dateInterval, DateTime start, DateTime ends) => throw new NotSupportedException(MESSAGE);

        #region SUM

        public static Byte Sum(Byte values) => throw new NotSupportedException(MESSAGE);

        public static Int16 Sum(Int16 values) => throw new NotSupportedException(MESSAGE);

        public static Int32 Sum(Int32 values) => throw new NotSupportedException(MESSAGE);

        public static Int64 Sum(Int64 values) => throw new NotSupportedException(MESSAGE);

        public static UInt16 Sum(UInt16 values) => throw new NotSupportedException(MESSAGE);

        public static UInt32 Sum(UInt32 values) => throw new NotSupportedException(MESSAGE);

        public static UInt64 Sum(UInt64 values) => throw new NotSupportedException(MESSAGE);

        public static Single Sum(Single values) => throw new NotSupportedException(MESSAGE);

        public static Double Sum(Double values) => throw new NotSupportedException(MESSAGE);

        public static Decimal Sum(Decimal values) => throw new NotSupportedException(MESSAGE);

        #endregion

        #region AVG

        public static Double Avg(Byte values) => throw new NotSupportedException(MESSAGE);

        public static Double Avg(Int16 values) => throw new NotSupportedException(MESSAGE);

        public static Double Avg(Int32 values) => throw new NotSupportedException(MESSAGE);

        public static Double Avg(Int64 values) => throw new NotSupportedException(MESSAGE);

        public static Double Avg(UInt16 values) => throw new NotSupportedException(MESSAGE);

        public static Double Avg(UInt32 values) => throw new NotSupportedException(MESSAGE);

        public static Double Avg(UInt64 values) => throw new NotSupportedException(MESSAGE);

        public static Single Avg(Single values) => throw new NotSupportedException(MESSAGE);

        public static Double Avg(Double values) => throw new NotSupportedException(MESSAGE);

        public static Decimal Avg(Decimal values) => throw new NotSupportedException(MESSAGE);

        #endregion
    }
}