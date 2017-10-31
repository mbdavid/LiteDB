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
        #region Year/Month/Day/Hour/Minute/Second

        /// <summary>
        /// Get year from date. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> YEAR(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Year;
            }
        }

        /// <summary>
        /// Get month from date. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> MONTH(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Month;
            }
        }

        /// <summary>
        /// Get day from date. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> DAY(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Day;
            }
        }

        /// <summary>
        /// Get hour from date. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> HOUR(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Hour;
            }
        }

        /// <summary>
        /// Get minute from date. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> MINUTE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Minute;
            }
        }

        /// <summary>
        /// Get seconds from date. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> SECOND(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsDateTime).Select(x => x.AsDateTime))
            {
                yield return value.Second;
            }
        }

        #endregion

        /// <summary>
        /// Add an interval to date. Use dateParts: "y|year", "M|month", "d|day", "h|hour", "m|minute", "s|second". Support multi values
        /// </summary>
        public static IEnumerable<BsonValue> DATEADD(IEnumerable<BsonValue> dateParts, IEnumerable<BsonValue> numbers, IEnumerable<BsonValue> values)
        {
            foreach (var value in dateParts.ZipValues(numbers, values))
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
        /// Returns an interval about 2 dates. Use dateParts: "y|year", "M|month", "d|day", "h|hour", "m|minute", "s|second". Support multi values
        /// </summary>
        public static IEnumerable<BsonValue> DATEDIFF(IEnumerable<BsonValue> dateParts, IEnumerable<BsonValue> starts, IEnumerable<BsonValue> ends)
        {
            foreach (var value in dateParts.ZipValues(starts, ends))
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
    }
}
