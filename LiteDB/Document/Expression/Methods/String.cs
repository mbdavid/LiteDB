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
        /// <summary>
        /// Return lower case from string value. Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> LOWER(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsString)
                {
                    yield return value.AsString.ToLower();
                }
            }
        }

        /// <summary>
        /// Return UPPER case from string value. Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> UPPER(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsString)
                {
                    yield return value.AsString.ToUpper();
                }
            }
        }

        /// <summary>
        /// Returns substring from string value using index and length (zero-based). Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> SUBSTRING(IEnumerable<BsonValue> values, IEnumerable<BsonValue> index)
        {
            var idx = index?.Where(x => x.IsInt32).FirstOrDefault()?.AsInt32 ?? 0;

            foreach (var value in values)
            {
                if (value.IsString)
                {
                    yield return value.AsString.Substring(idx);
                }
            }
        }

        /// <summary>
        /// Returns substring from string value using index and length (zero-based). Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> SUBSTRING(IEnumerable<BsonValue> values, IEnumerable<BsonValue> index, IEnumerable<BsonValue> length)
        {
            var idx = index?.Where(x => x.IsInt32).FirstOrDefault()?.AsInt32 ?? 0;
            var len = length?.Where(x => x.IsInt32).FirstOrDefault()?.AsInt32 ?? 0;

            foreach (var value in values)
            {
                if (value.IsString)
                {
                    yield return value.AsString.Substring(idx, len);
                }
            }
        }

        /// <summary>
        /// Return value string with left padding. Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> LPAD(IEnumerable<BsonValue> values, IEnumerable<BsonValue> totalWidth, IEnumerable<BsonValue> paddingChar)
        {
            var width = totalWidth?.Where(x => x.IsInt32).FirstOrDefault()?.AsInt32 ?? 0;
            var pchar = paddingChar?.Where(x => x.IsString).FirstOrDefault()?.AsString.ToCharArray()[0] ?? '0';

            foreach (var value in values)
            {
                yield return value.AsString.PadLeft(width, pchar);
            }
        }


        /// <summary>
        /// Return value string with right padding. Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> RPAD(IEnumerable<BsonValue> values, IEnumerable<BsonValue> totalWidth, IEnumerable<BsonValue> paddingChar)
        {
            var width = totalWidth?.Where(x => x.IsInt32).FirstOrDefault()?.AsInt32 ?? 0;
            var pchar = paddingChar?.Where(x => x.IsString).FirstOrDefault()?.AsString.ToCharArray()[0] ?? '0';

            foreach (var value in values)
            {
                yield return value.AsString.PadRight(width, pchar);
            }
        }

        /// <summary>
        /// Return format value string using format definition (same as String.Format("{0:~}", values)). Support multiple values (only string)
        /// </summary>
        public static IEnumerable<BsonValue> FORMAT(IEnumerable<BsonValue> values, IEnumerable<BsonValue> format)
        {
            foreach (var value in values.ZipValues(format).Where(x => x.Second.IsString))
            {
                yield return string.Format("{0:" + value.Second.AsString + "}", value.First.RawValue);
            }
        }
    }
}
