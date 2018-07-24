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
        #region Year/Month/Day/Hour/Minute/Second

        /// <summary>
        /// Get year from date
        /// </summary>
        public static IEnumerable<BsonValue> YEAR(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Year;
            }
        }

        /// <summary>
        /// Get month from date
        /// </summary>
        public static IEnumerable<BsonValue> MONTH(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Month;
            }
        }

        /// <summary>
        /// Get day from date
        /// </summary>
        public static IEnumerable<BsonValue> DAY(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Day;
            }
        }

        /// <summary>
        /// Get hour from date
        /// </summary>
        public static IEnumerable<BsonValue> HOUR(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Hour;
            }
        }

        /// <summary>
        /// Get minute from date
        /// </summary>
        public static IEnumerable<BsonValue> MINUTE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Minute;
            }
        }

        /// <summary>
        /// Get seconds from date
        /// </summary>
        public static IEnumerable<BsonValue> SECOND(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Second;
            }
        }

        #endregion

        #region Date Functions

        /// <summary>
        /// Add an interval to date. Use dateInterval: "y" (or "year"), "M" (or "month"), "d" (or "day"), "h" (or "hour"), "m" (or "minute"), "s" or ("second")
        /// </summary>
        public static IEnumerable<BsonValue> DATEADD(IEnumerable<BsonValue> dateInterval, IEnumerable<BsonValue> numbers, IEnumerable<BsonValue> values)
        {
            foreach (var value in ZipValues(dateInterval, numbers, values))
            {
                if (!value.First.IsString || !value.Second.IsNumber || !value.Third.IsDateTime) continue;

                var datePart = value.First.AsString;
                var number = value.Second.AsInt32;
                var date = value.Third.AsDateTime;

                datePart = datePart == "M" ? "month" : datePart.ToLower();

                if (datePart == "y" || datePart== "year") yield return date.AddYears(number);
                else if (datePart == "month") yield return date.AddMonths(number);
                else if (datePart == "d" || datePart == "day") yield return date.AddDays(number);
                else if (datePart == "h" || datePart == "hour") yield return date.AddHours(number);
                else if (datePart == "m" || datePart == "minute") yield return date.AddMinutes(number);
                else if (datePart == "s" || datePart == "second") yield return date.AddSeconds(number);
            }
        }

        /// <summary>
        /// Returns an interval about 2 dates. Use dateInterval: "y|year", "M|month", "d|day", "h|hour", "m|minute", "s|second"
        /// </summary>
        public static IEnumerable<BsonValue> DATEDIFF(IEnumerable<BsonValue> dateInterval, IEnumerable<BsonValue> starts, IEnumerable<BsonValue> ends)
        {
            foreach (var value in ZipValues(dateInterval, starts, ends))
            {
                if (!value.First.IsString || !value.Second.IsDateTime || !value.Third.IsDateTime) continue;

                var datePart = value.First.AsString;
                var start = value.Second.AsDateTime;
                var end = value.Third.AsDateTime;

                datePart = datePart == "M" ? "month" : datePart.ToLower();

                if (datePart == "y" || datePart == "year") yield return start.YearDifference(end);
                else if (datePart == "month") yield return start.MonthDifference(end);
                else if (datePart == "d" || datePart == "day") yield return Convert.ToInt32(Math.Truncate(end.Subtract(start).TotalDays));
                else if (datePart == "h" || datePart == "hour") yield return Convert.ToInt32(Math.Truncate(end.Subtract(start).TotalHours));
                else if (datePart == "m" || datePart == "minute") yield return Convert.ToInt32(Math.Truncate(end.Subtract(start).TotalMinutes));
                else if (datePart == "s" || datePart == "second") yield return Convert.ToInt32(Math.Truncate(end.Subtract(start).TotalSeconds));
            }
        }

        #endregion
    }
}
