using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    public partial class BsonExpression
    {
        /// <summary>
        /// Parse a JSON string into a new BsonValue. Support multiple values (string only)
        /// JSON('{a:1}') = {a:1}
        /// </summary>
        public static IEnumerable<BsonValue> JSON(IEnumerable<BsonValue> values)
        {
            foreach (var value in values.Where(x => x.IsString))
            {
                yield return JsonSerializer.Deserialize(value);
            }
        }

        /// <summary>
        /// Extend source document with other document. Copy all field from extend to source. Source document will be modified.
        /// EXTEND($, {a: 2}) = {_id:1, a: 2}
        /// </summary>
        public static IEnumerable<BsonValue> EXTEND(IEnumerable<BsonValue> source, IEnumerable<BsonValue> extend)
        {
            foreach (var value in source.ZipValues(extend))
            {
                if (!value.First.IsDocument) continue;
                if (!value.Second.IsDocument) continue;

                var dest = value.First.AsDocument;

                value.Second.AsDocument.CopyTo(dest);

                yield return dest;
            }
        }

        /// <summary>
        /// Convert an array into IEnuemrable of values.
        /// ITEMS([1, 2, null]) = 1, 2, null
        /// </summary>
        public static IEnumerable<BsonValue> ITEMS(IEnumerable<BsonValue> array)
        {
            foreach (var arr in array.Where(x => x.IsArray).Select(x => x.AsArray))
            {
                foreach(var value in arr)
                {
                    yield return value;
                }
            }
        }

        #region Convert datatypes

        /// <summary>
        /// Return a new instance of MINVALUE
        /// </summary>
        public static IEnumerable<BsonValue> MINVALUE()
        {
            yield return BsonValue.MinValue;
        }

        // ==> "null" are a keyword

        /// <summary>
        /// Convert values into INT32. Returns empty if not possible to convert. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> INT32(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsNumber)
                {
                    yield return value.AsInt32;
                }
                else
                {
                    if (Int32.TryParse(value.AsString, out var val))
                    {
                        yield return val;
                    }
                }
            }
        }

        /// <summary>
        /// Convert values into INT64. Returns empty if not possible to convert. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> INT64(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsNumber)
                {
                    yield return value.AsInt64;
                }
                else
                {
                    if (Int64.TryParse(value.AsString, out var val))
                    {
                        yield return val;
                    }
                }
            }
        }

        /// <summary>
        /// Convert values into DOUBLE. Returns empty if not possible to convert. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> DOUBLE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsNumber)
                {
                    yield return value.AsDouble;
                }
                else
                {
                    if (Double.TryParse(value.AsString, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var val))
                    {
                        yield return val;
                    }
                }
            }
        }

        /// <summary>
        /// Convert values into DECIMAL. Returns empty if not possible to convert. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> DECIMAL(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsNumber)
                {
                    yield return value.AsDecimal;
                }
                else
                {
                    if (Decimal.TryParse(value.AsString, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var val))
                    {
                        yield return val;
                    }
                }
            }
        }

        /// <summary>
        /// Convert values into STRING. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> STRING(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.AsString;
            }
        }

        // ==> there is no convert to BsonDocument, must use { .. } syntax 

        /// <summary>
        /// Return an array from list of values. Support multiple values but returns a single value
        /// </summary>
        public static IEnumerable<BsonValue> ARRAY(IEnumerable<BsonValue> values)
        {
            yield return new BsonArray(values);
        }

        /// <summary>
        /// Create a new OBJECTID value
        /// </summary>
        public static IEnumerable<BsonValue> OBJECTID()
        {
            yield return ObjectId.NewObjectId();
        }

        /// <summary>
        /// Convert values into OBJECTID. Returns empty if not possible to convert. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> OBJECTID(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsObjectId)
                {
                    yield return value.AsObjectId;
                }
                else
                {
                    var val = ObjectId.Empty;
                    var isobjectid = false;

                    try
                    {
                        val = new ObjectId(value.AsString);
                        isobjectid = true;
                    }
                    catch
                    {
                    }

                    if (isobjectid) yield return val;
                }
            }
        }

        /// <summary>
        /// Create a new GUID value
        /// </summary>
        public static IEnumerable<BsonValue> GUID()
        {
            yield return Guid.NewGuid();
        }

        /// <summary>
        /// Convert values into GUID. Returns empty if not possible to convert. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> GUID(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsGuid)
                {
                    yield return value.AsGuid;
                }
                else
                {
                    var val = Guid.Empty;
                    var isguid = false;

                    try
                    {
                        val = new Guid(value.AsString);
                        isguid = true;
                    }
                    catch
                    {
                    }

                    if (isguid) yield return val;
                }
            }
        }

        /// <summary>
        /// Return a new DATETIME (Now)
        /// </summary>
        public static IEnumerable<BsonValue> DATETIME()
        {
            yield return DateTime.Now;
        }

        /// <summary>
        /// Convert values into DATETIME. Returns empty if not possible to convert. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> DATETIME(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsDateTime)
                {
                    yield return value.AsDateTime;
                }
                else
                {
                    if (DateTime.TryParse(value.AsString, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out var val))
                    {
                        yield return val;
                    }
                }
            }
        }

        /// <summary>
        /// Create a new instance of DATETIME based on year, month, day
        /// </summary>
        public static IEnumerable<BsonValue> DATETIME(IEnumerable<BsonValue> year, IEnumerable<BsonValue> month, IEnumerable<BsonValue> day)
        {
            foreach (var value in year.ZipValues(month, day))
            {
                if (value.First.IsNumber && value.Second.IsNumber && value.Third.IsNumber)
                {
                    yield return new DateTime(value.First.AsInt32, value.Second.AsInt32, value.Third.AsInt32);
                }
            }
        }

        /// <summary>
        /// Return a new instance of MAXVALUE
        /// </summary>
        public static IEnumerable<BsonValue> MAXVALUE()
        {
            yield return BsonValue.MaxValue;
        }

        #endregion

        #region IS_DATETYPE

        /// <summary>
        /// Return true if value is MINVALUE. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_MINVALUE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsMinValue;
            }
        }

        /// <summary>
        /// Return true if value is NULL. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_NULL(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsNull;
            }
        }

        /// <summary>
        /// Return true if value is INT32. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_INT32(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.AsInt32;
            }
        }

        /// <summary>
        /// Return true if value is INT64. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_INT64(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsInt64;
            }
        }

        /// <summary>
        /// Return true if value is DOUBLE. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_DOUBLE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDouble;
            }
        }


        /// <summary>
        /// Return true if value is DECIMAL. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_DECIMAL(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDecimal;
            }
        }
        
        /// <summary>
                 /// Return true if value is NUMBER (int, double, decimal). Support multiple values
                 /// </summary>
        public static IEnumerable<BsonValue> IS_NUMBER(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsNumber;
            }
        }

        /// <summary>
        /// Return true if value is STRING. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_STRING(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsString;
            }
        }

        /// <summary>
        /// Return true if value is DOCUMENT. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_DOCUMENT(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDocument;
            }
        }

        /// <summary>
        /// Return true if value is ARRAY. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_ARRAY(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsArray;
            }
        }

        /// <summary>
        /// Return true if value is BINARY. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_BINARY(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsBinary;
            }
        }

        /// <summary>
        /// Return true if value is OBJECTID. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_OBJECTID(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsObjectId;
            }
        }

        /// <summary>
        /// Return true if value is GUID. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_GUID(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsGuid;
            }
        }

        /// <summary>
        /// Return true if value is BOOLEAN. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_BOOLEAN(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsBoolean;
            }
        }

        /// <summary>
        /// Return true if value is DATETIME. Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_DATETIME(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDateTime;
            }
        }

        /// <summary>
        /// Return true if value is DATE (alias to DATETIME). Support multiple values
        /// </summary>
        public static IEnumerable<BsonValue> IS_MAXVALUE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsMaxValue;
            }
        }

        #endregion

        #region ALIAS

        /// <summary>
        /// Alias to INT32(values)
        /// </summary>
        public static IEnumerable<BsonValue> INT(IEnumerable<BsonValue> values) => INT32(values);

        /// <summary>
        /// Alias to INT64(values)
        /// </summary>
        public static IEnumerable<BsonValue> LONG(IEnumerable<BsonValue> values) => INT64(values);


        /// <summary>
        /// Alias to DATETIME()
        /// </summary>
        public static IEnumerable<BsonValue> DATE() => DATETIME();
        public static IEnumerable<BsonValue> DATE(IEnumerable<BsonValue> values) => DATETIME(values);
        public static IEnumerable<BsonValue> DATE(IEnumerable<BsonValue> year, IEnumerable<BsonValue> month, IEnumerable<BsonValue> day) => DATETIME(year, month, day);

        /// <summary>
        /// Alias to IS_INT32(values)
        /// </summary>
        public static IEnumerable<BsonValue> IS_INT(IEnumerable<BsonValue> values) => IS_INT32(values);

        /// <summary>
        /// Alias to IS_INT64(values)
        /// </summary>
        public static IEnumerable<BsonValue> IS_LONG(IEnumerable<BsonValue> values) => IS_INT64(values);

        /// <summary>
        /// Alias to IS_BOOLEAN(values)
        /// </summary>
        public static IEnumerable<BsonValue> IS_BOOL(IEnumerable<BsonValue> values) => IS_BOOLEAN(values);

        /// <summary>
        /// Alias to IS_DATE(values)
        /// </summary>
        public static IEnumerable<BsonValue> IS_DATE(IEnumerable<BsonValue> values) => IS_DATETIME(values);

        #endregion
    }
}
