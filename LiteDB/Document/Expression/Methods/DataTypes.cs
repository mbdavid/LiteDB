using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static LiteDB.ZipExtensions;

namespace LiteDB
{
    internal partial class BsonExpressionMethods
    {
        #region NEW_INSTANCE

        /// <summary>
        /// Return a new instance of MINVALUE
        /// </summary>
        public static IEnumerable<BsonValue> MINVALUE()
        {
            yield return BsonValue.MinValue;
        }

        /// <summary>
        /// Create a new OBJECTID value
        /// </summary>
        [Volatile]
        public static IEnumerable<BsonValue> OBJECTID()
        {
            yield return ObjectId.NewObjectId();
        }

        /// <summary>
        /// Create a new GUID value
        /// </summary>
        [Volatile]
        public static IEnumerable<BsonValue> GUID()
        {
            yield return Guid.NewGuid();
        }

        /// <summary>
        /// Return a new DATETIME (Now)
        /// </summary>
        [Volatile]
        public static IEnumerable<BsonValue> NOW()
        {
            yield return DateTime.Now;
        }

        /// <summary>
        /// Return a new DATETIME (UtcNow)
        /// </summary>
        [Volatile]
        public static IEnumerable<BsonValue> NOW_UTC()
        {
            yield return DateTime.UtcNow;
        }

        /// <summary>
        /// Return a new DATETIME (Today)
        /// </summary>
        [Volatile]
        public static IEnumerable<BsonValue> TODAY()
        {
            yield return DateTime.Today;
        }

        /// <summary>
        /// Return a new instance of MAXVALUE
        /// </summary>
        public static IEnumerable<BsonValue> MAXVALUE()
        {
            yield return BsonValue.MaxValue;
        }

        #endregion

        #region DATATYPE

        // ==> MaxValue is a constant
        // ==> "null" are a keyword

        /// <summary>
        /// Convert values into INT32. Returns empty if not possible to convert
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
        /// Convert values into INT64. Returns empty if not possible to convert
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
        /// Convert values into DOUBLE. Returns empty if not possible to convert
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
        /// Convert values into DECIMAL. Returns empty if not possible to convert
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
        /// Convert values into STRING
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
        /// Return an binary from string (base64) values
        /// </summary>
        public static IEnumerable<BsonValue> BINARY(IEnumerable<BsonValue> values)
        {
            foreach(var value in values)
            {
                if (value.IsBinary)
                {
                    yield return value;
                }
                else if (value.IsString)
                {
                    byte[] data = null;
                    var isBase64 = false;

                    try
                    {
                        data = Convert.FromBase64String(value.AsString);
                        isBase64 = true;
                    }
                    catch (FormatException)
                    {
                    }

                    if (isBase64) yield return data;
                }
            }
        }

        /// <summary>
        /// Convert values into OBJECTID. Returns empty if not possible to convert
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
                    ObjectId val = null;
                    var isObjectId = false;

                    try
                    {
                        val = new ObjectId(value.AsString);
                        isObjectId = true;
                    }
                    catch
                    {
                    }

                    if (isObjectId) yield return val;
                }
            }
        }

        /// <summary>
        /// Convert values into GUID. Returns empty if not possible to convert
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
                    var isGuid = false;

                    try
                    {
                        val = new Guid(value.AsString);
                        isGuid = true;
                    }
                    catch
                    {
                    }

                    if (isGuid) yield return val;
                }
            }
        }

        /// <summary>
        /// Return converted value into BOOLEAN value
        /// </summary>
        public static IEnumerable<BsonValue> BOOLEAN(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsBoolean)
                {
                    yield return value.AsBoolean;
                }
                else
                {
                    var val = false;
                    var isBool = false;

                    try
                    {
                        val = Convert.ToBoolean(value.AsString);
                        isBool = true;
                    }
                    catch
                    {
                    }

                    if (isBool) yield return val;
                }
            }
        }

        /// <summary>
        /// Convert values into DATETIME. Returns empty if not possible to convert
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
        /// Convert values into DATETIME. Returns empty if not possible to convert
        /// </summary>
        public static IEnumerable<BsonValue> DATETIME_UTC(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                if (value.IsDateTime)
                {
                    yield return value.AsDateTime;
                }
                else
                {
                    if (DateTime.TryParse(value.AsString, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal, out var val))
                    {
                        yield return val;
                    }
                }
            }
        }
        /// <summary>
        /// Create a new instance of DATETIME based on year, month, day (local time)
        /// </summary>
        public static IEnumerable<BsonValue> DATETIME(IEnumerable<BsonValue> year, IEnumerable<BsonValue> month, IEnumerable<BsonValue> day)
        {
            foreach (var value in ZipValues(year, month, day))
            {
                if (value.First.IsNumber && value.Second.IsNumber && value.Third.IsNumber)
                {
                    yield return new DateTime(value.First.AsInt32, value.Second.AsInt32, value.Third.AsInt32);
                }
            }
        }

        /// <summary>
        /// Create a new instance of DATETIME based on year, month, day (UTC)
        /// </summary>
        public static IEnumerable<BsonValue> DATETIME_UTC(IEnumerable<BsonValue> year, IEnumerable<BsonValue> month, IEnumerable<BsonValue> day)
        {
            foreach (var value in ZipValues(year, month, day))
            {
                if (value.First.IsNumber && value.Second.IsNumber && value.Third.IsNumber)
                {
                    yield return new DateTime(value.First.AsInt32, value.Second.AsInt32, value.Third.AsInt32, 0, 0, 0, DateTimeKind.Utc);
                }
            }
        }

        #endregion

        #region IS_DATETYPE

        /// <summary>
        /// Return true if value is MINVALUE
        /// </summary>
        public static IEnumerable<BsonValue> IS_MINVALUE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsMinValue;
            }
        }

        /// <summary>
        /// Return true if value is NULL
        /// </summary>
        public static IEnumerable<BsonValue> IS_NULL(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsNull;
            }
        }

        /// <summary>
        /// Return true if value is INT32
        /// </summary>
        public static IEnumerable<BsonValue> IS_INT32(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.AsInt32;
            }
        }

        /// <summary>
        /// Return true if value is INT64
        /// </summary>
        public static IEnumerable<BsonValue> IS_INT64(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsInt64;
            }
        }

        /// <summary>
        /// Return true if value is DOUBLE
        /// </summary>
        public static IEnumerable<BsonValue> IS_DOUBLE(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDouble;
            }
        }

        /// <summary>
        /// Return true if value is DECIMAL
        /// </summary>
        public static IEnumerable<BsonValue> IS_DECIMAL(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDecimal;
            }
        }
        
        /// <summary>
        /// Return true if value is NUMBER (int, double, decimal)
        /// </summary>
        public static IEnumerable<BsonValue> IS_NUMBER(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsNumber;
            }
        }

        /// <summary>
        /// Return true if value is STRING
        /// </summary>
        public static IEnumerable<BsonValue> IS_STRING(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsString;
            }
        }

        /// <summary>
        /// Return true if value is DOCUMENT
        /// </summary>
        public static IEnumerable<BsonValue> IS_DOCUMENT(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDocument;
            }
        }

        /// <summary>
        /// Return true if value is ARRAY
        /// </summary>
        public static IEnumerable<BsonValue> IS_ARRAY(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsArray;
            }
        }

        /// <summary>
        /// Return true if value is BINARY
        /// </summary>
        public static IEnumerable<BsonValue> IS_BINARY(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsBinary;
            }
        }

        /// <summary>
        /// Return true if value is OBJECTID
        /// </summary>
        public static IEnumerable<BsonValue> IS_OBJECTID(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsObjectId;
            }
        }

        /// <summary>
        /// Return true if value is GUID
        /// </summary>
        public static IEnumerable<BsonValue> IS_GUID(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsGuid;
            }
        }

        /// <summary>
        /// Return true if value is BOOLEAN
        /// </summary>
        public static IEnumerable<BsonValue> IS_BOOLEAN(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsBoolean;
            }
        }

        /// <summary>
        /// Return true if value is DATETIME
        /// </summary>
        public static IEnumerable<BsonValue> IS_DATETIME(IEnumerable<BsonValue> values)
        {
            foreach (var value in values)
            {
                yield return value.IsDateTime;
            }
        }

        /// <summary>
        /// Return true if value is DATE (alias to DATETIME)
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
        /// Alias to DATETIME(values) and DATETIME_UTC(values)
        /// </summary>
        public static IEnumerable<BsonValue> DATE(IEnumerable<BsonValue> values) => DATETIME(values);
        public static IEnumerable<BsonValue> DATE_UTC(IEnumerable<BsonValue> values) => DATETIME_UTC(values);

        public static IEnumerable<BsonValue> DATE(IEnumerable<BsonValue> year, IEnumerable<BsonValue> month, IEnumerable<BsonValue> day) => DATETIME(year, month, day);
        public static IEnumerable<BsonValue> DATE_UTC(IEnumerable<BsonValue> year, IEnumerable<BsonValue> month, IEnumerable<BsonValue> day) => DATETIME_UTC(year, month, day);

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
