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
        /// <summary>
        /// Return lower case from string value. Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> LOWER(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsString).Select(x => x.AsString))
            {
                yield return value.ToLowerInvariant();
            }
        }

        /// <summary>
        /// Return UPPER case from string value. Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> UPPER(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsString).Select(x => x.AsString))
            {
                yield return value.ToUpperInvariant();
            }
        }

        /// <summary>
        /// Returns substring from string value using index and length (zero-based). Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> SUBSTRING(IEnumerable<BsonValue> values, IEnumerable<BsonValue> index)
        {
            foreach (var value in ZipValues(values, index))
            {
                if (!value.First.IsString) continue;
                if (!value.Second.IsNumber) continue;

                yield return value.First.AsString.Substring(value.Second.AsInt32);
            }
        }

        /// <summary>
        /// Returns substring from string value using index and length (zero-based). Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> SUBSTRING(IEnumerable<BsonValue> values, IEnumerable<BsonValue> index, IEnumerable<BsonValue> length)
        {
            foreach (var value in ZipValues(values, index))
            {
                if (!value.First.IsString) continue;
                if (!value.Second.IsNumber) continue;
                if (!value.Third.IsNumber) continue;

                yield return value.First.AsString.Substring(value.Second.AsInt32, value.Third.AsInt32);
            }
        }

        /// <summary>
        /// Returns replaced string changing oldValue with newValue. Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> REPLACE(IEnumerable<BsonValue> values, IEnumerable<BsonValue> oldValues, IEnumerable<BsonValue> newValues)
        {
            foreach (var value in ZipValues(values, oldValues, newValues))
            {
                if (!value.First.IsString) continue;
                if (!value.Second.IsString) continue;
                if (!value.Third.IsString) continue;

                yield return value.First.AsString.Replace(value.Second.AsString, value.Third.AsString);
            }
        }

        /// <summary>
        /// Return value string with left padding. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> LPAD(IEnumerable<BsonValue> values, IEnumerable<BsonValue> totalWidth, IEnumerable<BsonValue> paddingChar)
        {
            foreach (var value in ZipValues(values, totalWidth, paddingChar))
            {
                if (!value.First.IsString) continue;
                if (!value.Second.IsNumber) continue;
                if (!value.Third.IsString) continue;

                yield return value.First.AsString.PadLeft(value.Second.AsInt32, value.Third.AsString.ToCharArray()[0]);
            }
        }


        /// <summary>
        /// Return value string with right padding. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> RPAD(IEnumerable<BsonValue> values, IEnumerable<BsonValue> totalWidth, IEnumerable<BsonValue> paddingChar)
        {
            foreach (var value in ZipValues(values, totalWidth, paddingChar))
            {
                if (!value.First.IsString) continue;
                if (!value.Second.IsNumber) continue;
                if (!value.Third.IsString) continue;

                yield return value.First.AsString.PadRight(value.Second.AsInt32, value.Third.AsString.ToCharArray()[0]);
            }
        }

        /// <summary>
        /// Return format value string using format definition (same as String.Format("{0:~}", values)). Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> FORMAT(IEnumerable<BsonValue> values, IEnumerable<BsonValue> format)
        {
            foreach (var value in ZipValues(values, format))
            {
                if (!value.Second.IsString) continue;

                yield return string.Format("{0:" + value.Second.AsString + "}", value.First.RawValue);
            }
        }
    }
}
