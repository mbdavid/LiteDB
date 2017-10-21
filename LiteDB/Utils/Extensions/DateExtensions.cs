using System;

namespace LiteDB
{
    internal static class DateExtensions
    {
        /// <summary>
        /// Truncate DateTime in milliseconds
        /// </summary>
        public static DateTime Truncate(this DateTime dt)
        {
            if (dt == DateTime.MaxValue || dt == DateTime.MinValue)
            {
                return dt;
            }

            return new DateTime(dt.Year, dt.Month, dt.Day,
                dt.Hour, dt.Minute, dt.Second, dt.Millisecond, 
                dt.Kind);
        }
    }
}