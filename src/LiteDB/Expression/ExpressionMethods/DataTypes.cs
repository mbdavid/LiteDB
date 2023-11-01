namespace LiteDB;

internal partial class BsonExpressionMethods
{
    #region NEW_INSTANCE

    /// <summary>
    /// Return a new instance of MINVALUE
    /// </summary>
    public static BsonValue MINVALUE() =>  BsonValue.MinValue;

    /// <summary>
    /// Create a new OBJECTID value
    /// </summary>
    [Volatile]
    public static BsonValue OBJECTID() => ObjectId.NewObjectId();

    /// <summary>
    /// Create a new GUID value
    /// </summary>
    [Volatile]
    public static BsonValue GUID() =>  Guid.NewGuid();

    /// <summary>
    /// Return a new DATETIME (Now)
    /// </summary>
    [Volatile]
    public static BsonValue NOW() => DateTime.Now;

    /// <summary>
    /// Return a new DATETIME (UtcNow)
    /// </summary>
    [Volatile]
    public static BsonValue NOW_UTC() => DateTime.UtcNow;

    /// <summary>
    /// Return a new DATETIME (Today)
    /// </summary>
    [Volatile]
    public static BsonValue TODAY() =>  DateTime.Today;

    /// <summary>
    /// Return a new instance of MAXVALUE
    /// </summary>
    public static BsonValue MAXVALUE() => BsonValue.MaxValue;

    #endregion

    #region DATATYPE

    // ==> MaxValue is a constant
    // ==> "null" are a keyword

    /// <summary>
    /// Convert values into INT32. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue INT32(BsonValue value)
    {
        if (value.IsNumber) return value.ToInt32();

        if (value is BsonString str)
        {
            if (int.TryParse(str.Value, out var val))
            {
                return val;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into INT64. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue INT64(BsonValue value)
    {
        if (value.IsNumber) return value.ToInt64();

        if (value is BsonString str)
        {
            if (long.TryParse(str.Value, out var val))
            {
                return val;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into DOUBLE. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue DOUBLE(Collation collation, BsonValue value)
    {
        if (value.IsNumber) return value.ToDouble();

        if (value is BsonString str)
        {
            if (double.TryParse(str.Value, NumberStyles.Any, collation.Culture.NumberFormat, out var val))
            {
                return val;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into DOUBLE. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue DOUBLE(BsonValue value, BsonValue culture)
    {
        if (value.IsNumber) return value.ToDouble();

        if (value is BsonString str && culture is BsonString cult)
        {
            //TODO: re-use same cultureinfo instance
            var cultureInfo = new CultureInfo(cult.Value); // en-US

            if (double.TryParse(str, NumberStyles.Any, cultureInfo.NumberFormat, out var val))
            {
                return val;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into DECIMAL. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue DECIMAL(Collation collation, BsonValue value)
    {
        if (value.IsNumber) return value.ToDecimal();

        if (value is BsonString str)
        {
            if (decimal.TryParse(str, NumberStyles.Any, collation.Culture.NumberFormat, out var val))
            {
                return val;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into DECIMAL. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue DECIMAL(BsonValue value, BsonValue culture)
    {
        if (value.IsNumber) return value.ToDecimal();

        if (value is BsonString str && culture is BsonString cult)
        {
            var cultureInfo = new CultureInfo(cult.Value); // en-US

            if (decimal.TryParse(str, NumberStyles.Any, cultureInfo.NumberFormat, out var val))
            {
                return val;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert value into STRING
    /// </summary>
    public static BsonValue STRING(BsonValue value)
    {
        return
            value.IsNull ? BsonString.Emtpy :
            value.IsString ? value :
            value.ToString();
    }

    /// <summary>
    /// Return an binary from string (base64) values
    /// </summary>
    public static BsonValue BINARY(BsonValue value)
    {
        if (value.IsBinary) return value;

        if (value is BsonString str)
        {
            try
            {
                var data = Convert.FromBase64String(str);

                return data;
            }
            catch (FormatException)
            {
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into OBJECTID. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue OBJECTID(BsonValue value)
    {
        if (value.IsObjectId) return value;

        if(value is BsonString str)
        {
            try
            {
                var val = new ObjectId(str.Value);
                
                return val;
            }
            catch
            {
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into GUID. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue GUID(BsonValue value)
    {
        if (value.IsGuid) return value;

        if(value is BsonString str)
        {
            if (Guid.TryParse(str, out var guid))
            {
                return guid;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Return converted value into BOOLEAN value
    /// </summary>
    public static BsonValue BOOLEAN(BsonValue value)
    {
        return value.ToBoolean();
    }

    /// <summary>
    /// Convert values into DATETIME. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue DATETIME(Collation collation, BsonValue value)
    {
        if (value.IsDateTime) return value.AsDateTime;

        if (value is BsonString str)
        {
            if (DateTime.TryParse(str.Value, collation.Culture.DateTimeFormat, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into DATETIME. Returns empty if not possible to convert. Support custom culture info
    /// </summary>
    public static BsonValue DATETIME(BsonValue value, BsonValue culture)
    {
        if (value.IsDateTime) return value.AsDateTime;

        if (value is BsonString str && culture is BsonString cult)
        {
            var cultureIndo = new CultureInfo(cult.Value); // en-US

            if (DateTime.TryParse(str, cultureIndo.DateTimeFormat, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into DATETIME. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue DATETIME_UTC(Collation collation, BsonValue value)
    {
        if (value.IsDateTime) return value.AsDateTime;

        if (value is BsonString str)
        {
            if (DateTime.TryParse(str.Value, collation.Culture.DateTimeFormat, DateTimeStyles.AssumeUniversal, out var date))
            {
                return date;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Convert values into DATETIME. Returns empty if not possible to convert
    /// </summary>
    public static BsonValue DATETIME_UTC(BsonValue value, BsonValue culture)
    {
        if (value.IsDateTime) return value.AsDateTime;

        if (value is BsonString str && culture is BsonString cult)
        {
            var cultureInfo = new CultureInfo(cult.Value); // en-US

            if (DateTime.TryParse(str, cultureInfo.DateTimeFormat, DateTimeStyles.AssumeUniversal, out var date))
            {
                return date;
            }
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Create a new instance of DATETIME based on year, month, day (local time)
    /// </summary>
    public static BsonValue DATETIME(BsonValue year, BsonValue month, BsonValue day)
    {
        if (year.IsNumber && month.IsNumber && day.IsNumber)
        {
            return new DateTime(year.ToInt32(), month.ToInt32(), day.ToInt32());
        }

        return BsonValue.Null;
    }

    /// <summary>
    /// Create a new instance of DATETIME based on year, month, day (UTC)
    /// </summary>
    public static BsonValue DATETIME_UTC(BsonValue year, BsonValue month, BsonValue day)
    {
        if (year.IsNumber && month.IsNumber && day.IsNumber)
        {
            return new DateTime(year.ToInt32(), month.ToInt32(), day.ToInt32(), 0, 0, 0, DateTimeKind.Utc);
        }

        return BsonValue.Null;
    }

    #endregion

    #region IS_DATETYPE

    /// <summary>
    /// Return true if value is MINVALUE
    /// </summary>
    public static BsonValue IS_MINVALUE(BsonValue value) => value.IsMinValue;

    /// <summary>
    /// Return true if value is NULL
    /// </summary>
    public static BsonValue IS_NULL(BsonValue value) => value.IsNull;

    /// <summary>
    /// Return true if value is INT32
    /// </summary>
    public static BsonValue IS_INT32(BsonValue value) => value.IsInt32;

    /// <summary>
    /// Return true if value is INT64
    /// </summary>
    public static BsonValue IS_INT64(BsonValue value) => value.IsInt64;

    /// <summary>
    /// Return true if value is DOUBLE
    /// </summary>
    public static BsonValue IS_DOUBLE(BsonValue value) => value.IsDouble;

    /// <summary>
    /// Return true if value is DECIMAL
    /// </summary>
    public static BsonValue IS_DECIMAL(BsonValue value) => value.IsDecimal;
        
    /// <summary>
    /// Return true if value is NUMBER (int, double, decimal)
    /// </summary>
    public static BsonValue IS_NUMBER(BsonValue value) => value.IsNumber;

    /// <summary>
    /// Return true if value is STRING
    /// </summary>
    public static BsonValue IS_STRING(BsonValue value) => value.IsString;

    /// <summary>
    /// Return true if value is DOCUMENT
    /// </summary>
    public static BsonValue IS_DOCUMENT(BsonValue value) => value.IsDocument;

    /// <summary>
    /// Return true if value is ARRAY
    /// </summary>
    public static BsonValue IS_ARRAY(BsonValue value) => value.IsArray;

    /// <summary>
    /// Return true if value is BINARY
    /// </summary>
    public static BsonValue IS_BINARY(BsonValue value) => value.IsBinary;

    /// <summary>
    /// Return true if value is OBJECTID
    /// </summary>
    public static BsonValue IS_OBJECTID(BsonValue value) =>  value.IsObjectId;

    /// <summary>
    /// Return true if value is GUID
    /// </summary>
    public static BsonValue IS_GUID(BsonValue value) => value.IsGuid;

    /// <summary>
    /// Return true if value is BOOLEAN
    /// </summary>
    public static BsonValue IS_BOOLEAN(BsonValue value) => value.IsBoolean;

    /// <summary>
    /// Return true if value is DATETIME
    /// </summary>
    public static BsonValue IS_DATETIME(BsonValue value) => value.IsDateTime;

    /// <summary>
    /// Return true if value is DATE (alias to DATETIME)
    /// </summary>
    public static BsonValue IS_MAXVALUE(BsonValue value) => value.IsMaxValue;

    #endregion

    #region ARRAY() DOCUMENT()

    public static BsonValue ARRAY(BsonValue values)
    {
        if (values.IsArray) return values;

        if (values is BsonDocument doc) return BsonArray.FromList(doc.Values);

        return BsonArray.Empty;
    }

    #endregion

    #region ALIAS

    /// <summary>
    /// Alias to INT32(values)
    /// </summary>
    public static BsonValue INT(BsonValue value) => INT32(value);

    /// <summary>
    /// Alias to INT64(values)
    /// </summary>
    public static BsonValue LONG(BsonValue value) => INT64(value);

    /// <summary>
    /// Alias to BOOLEAN(values)
    /// </summary>
    public static BsonValue BOOL(BsonValue value) => BOOLEAN(value);

    /// <summary>
    /// Alias to DATETIME(values) and DATETIME_UTC(values)
    /// </summary>
    public static BsonValue DATE(Collation collation, BsonValue value) => DATETIME(collation, value);
    public static BsonValue DATE(BsonValue values, BsonValue culture) => DATETIME(values, culture);
    public static BsonValue DATE_UTC(Collation collation, BsonValue value) => DATETIME_UTC(collation, value);
    public static BsonValue DATE_UTC(BsonValue values, BsonValue culture) => DATETIME_UTC(values, culture);

    public static BsonValue DATE(BsonValue year, BsonValue month, BsonValue day) => DATETIME(year, month, day);
    public static BsonValue DATE_UTC(BsonValue year, BsonValue month, BsonValue day) => DATETIME_UTC(year, month, day);

    /// <summary>
    /// Alias to IS_INT32(values)
    /// </summary>
    public static BsonValue IS_INT(BsonValue value) => IS_INT32(value);

    /// <summary>
    /// Alias to IS_INT64(values)
    /// </summary>
    public static BsonValue IS_LONG(BsonValue value) => IS_INT64(value);

    /// <summary>
    /// Alias to IS_BOOLEAN(values)
    /// </summary>
    public static BsonValue IS_BOOL(BsonValue value) => IS_BOOLEAN(value);

    /// <summary>
    /// Alias to IS_DATE(values)
    /// </summary>
    public static BsonValue IS_DATE(BsonValue value) => IS_DATETIME(value);

    #endregion
}
