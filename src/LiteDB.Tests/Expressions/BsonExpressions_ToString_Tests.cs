using static LiteDB.BsonExpression;

namespace LiteDB.Tests.Expressions;



public class BsonExpressions_ToString_Tests
{
    #region StaticAuxiliaryMethods
    private static BsonExpression Array(params BsonValue[] values)
    {
        var expressions = new List<BsonExpression>();
        foreach (BsonValue value in values)
        {
            expressions.Add(Constant(value));
        }
        return MakeArray(expressions);
    }
    #endregion

    public static IEnumerable<object[]> Get_Expressions()
    {
        #region BasicTypes
        yield return new object[] { Constant(10), "10" };
        yield return new object[] { Constant(2.6), "2.6" };
        yield return new object[] { Constant(true), "true" };
        yield return new object[] { Constant("LiteDB"), "\"LiteDB\"" };
        yield return new object[] { Array(12, 13, 14), "[12,13,14]" };
        yield return new object[] { MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }), "{a:1}" };
        yield return new object[] { Parameter("LiteDB"), "@LiteDB" };
        yield return new object[] { Root, "$" };
        yield return new object[] { Path(Root, "field"), "$.field" };
        #endregion

        #region InterTypesInteraction
        yield return new object[] { Add(Constant(12), Constant(14)), "12+14" };
        yield return new object[] { Add(Constant(2.9), Constant(3)), "2.9+3" };
        yield return new object[] { Add(Constant("Lite"), Constant("DB")), "\"Lite\"+\"DB\"" };
        yield return new object[] { Add(Constant(12), Constant("string")), "12+\"string\"" };
        yield return new object[] { Add(MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = "1" }), Constant("string")), "{a:1}+\"string\"" };
        yield return new object[] { Add(Constant(1), Constant("string")), "1+\"string\"" };
        yield return new object[] { Add(Array(1, 2), Constant(3)), "[1,2]+3" };
        #endregion

        #region DocumentRelated
        yield return new object[] { Path(Root, "clients"), "$.clients" };
        yield return new object[] { Path(Path(Root, "doc"), "arr"), "$.doc.arr" };
        yield return new object[] { Path(Current, "name"), "@.name" };
        yield return new object[] { Filter(Path(Root, "clients"), GreaterThanOrEqual(Path(Current, "age"), Constant(18))), "$.clients[@.age>=18]" };
        yield return new object[] { Map(Path(Root, "clients"), Path(Current, "name")), "$.clients=>@.name" };
        yield return new object[] { ArrayIndex(Path(Root, "arr"), Constant(1)), "$.arr[1]" };
        #endregion

        #region BinaryExpressions
        yield return new object[] { Add(Constant(1), Constant(2)), "1+2" };
        yield return new object[] { Subtract(Constant(1), Constant(2)), "1-2" };
        yield return new object[] { Multiply(Constant(1), Constant(2)), "1*2" };
        yield return new object[] { Divide(Constant(1), Constant(2)), "1/2" };
        yield return new object[] { Modulo(Constant(1), Constant(2)), "1%2" };
        yield return new object[] { Equal(Constant(1), Constant(2)), "1=2" };
        yield return new object[] { NotEqual(Constant(1), Constant(2)), "1!=2" };
        yield return new object[] { GreaterThan(Constant(1), Constant(2)), "1>2" };
        yield return new object[] { GreaterThanOrEqual(Constant(1), Constant(2)), "1>=2" };
        yield return new object[] { LessThan(Constant(1), Constant(2)), "1<2" };
        yield return new object[] { LessThanOrEqual(Constant(1), Constant(2)), "1<=2" };
        yield return new object[] { Contains(Array(1, 2), Constant(3)), "[1,2] CONTAINS 3" };
        yield return new object[] { Between(Constant(1), Array(2, 3)), "1 BETWEEN 2 AND 3" };
        yield return new object[] { Like(Constant("LiteDB"), Constant("L%")), "\"LiteDB\" LIKE \"L%\"" };
        yield return new object[] { In(Constant(1), Array(2, 3)), "1 IN [2,3]" };
        yield return new object[] { Or(Constant(true), Constant(false)), "true OR false" };
        yield return new object[] { And(Constant(true), Constant(false)), "true AND false" };
        #endregion

        #region CallMethods


        #region DataTypes
        #region NEW_INSTANCE
        yield return new object[] { Call("MINVALUE", new BsonExpression[] { }), "MINVALUE()" };
        yield return new object[] { Call("OBJECTID", new BsonExpression[] { }), "OBJECTID()" };
        yield return new object[] { Call("GUID", new BsonExpression[] { }), "GUID()" };
        yield return new object[] { Call("NOW", new BsonExpression[] { }), "NOW()" };
        yield return new object[] { Call("NOW_UTC", new BsonExpression[] { }), "NOW_UTC()" };
        yield return new object[] { Call("TODAY", new BsonExpression[] { }), "TODAY()" };
        yield return new object[] { Call("MAXVALUE", new BsonExpression[] { }), "MAXVALUE()" };
        #endregion

        #region DATATYPE
        yield return new object[] { Call("INT32", new BsonExpression[] { Constant(2.4) }), "INT32(2.4)" };
        yield return new object[] { Call("INT64", new BsonExpression[] { Constant(2) }), "INT64(2)" };
        yield return new object[] { Call("DOUBLE", new BsonExpression[] { Constant(2) }), "DOUBLE(2)" };
        yield return new object[] { Call("DECIMAL", new BsonExpression[] { Constant(2) }), "DECIMAL(2)" };
        yield return new object[] { Call("STRING", new BsonExpression[] { Constant(2) }), "STRING(2)" };
        yield return new object[] { Call("GUID", new BsonExpression[] { Constant("cf9fc62e-6a10-4e0b-b597-bcd7c19dddf5") }), "GUID(\"cf9fc62e-6a10-4e0b-b597-bcd7c19dddf5\")" };
        yield return new object[] { Call("BOOLEAN", new BsonExpression[] { Constant(true) }), "BOOLEAN(true)" };
        yield return new object[] { Call("DATETIME", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16))) }), "DATETIME({\"$date\":\"2000-10-16T03:00:00.0000000Z\"})" };
        yield return new object[] { Call("DATETIME_UTC", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16))) }), "DATETIME_UTC({\"$date\":\"2000-10-16T03:00:00.0000000Z\"})" };
        yield return new object[] { Call("DATETIME", new BsonExpression[] { Constant(2000), Constant(10), Constant(16) }), "DATETIME(2000,10,16)" };
        yield return new object[] { Call("DATETIME_UTC", new BsonExpression[] { Constant(2000), Constant(10), Constant(16) }), "DATETIME_UTC(2000,10,16)" };
        #endregion

        #region IS_DATETYPE
        yield return new object[] { Call("IS_MINVALUE", new BsonExpression[] { Constant(BsonValue.MinValue) }), "IS_MINVALUE({\"$minValue\":\"1\"})" };
        yield return new object[] { Call("IS_NULL", new BsonExpression[] { Constant(BsonValue.Null) }), "IS_NULL(null)" };
        yield return new object[] { Call("IS_INT32", new BsonExpression[] { Constant(2) }), "IS_INT32(2)" };
        yield return new object[] { Call("IS_INT64", new BsonExpression[] { Constant(new BsonInt64(2)) }), "IS_INT64({\"$numberLong\":\"2\"})" };
        yield return new object[] { Call("IS_DOUBLE", new BsonExpression[] { Constant(2.6) }), "IS_DOUBLE(2.6)" };
        yield return new object[] { Call("IS_DECIMAL", new BsonExpression[] { Constant(new BsonDecimal(2)) }), "IS_DECIMAL({\"$numberDecimal\":\"2\"})" };
        yield return new object[] { Call("IS_NUMBER", new BsonExpression[] { Constant(2) }), "IS_NUMBER(2)" };
        yield return new object[] { Call("IS_STRING", new BsonExpression[] { Constant("string") }), "IS_STRING(\"string\")" };
        yield return new object[] { Call("IS_DOCUMENT", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) }), "IS_DOCUMENT({name:\"Maria\",age:18})" };
        yield return new object[] { Call("IS_ARRAY", new BsonExpression[] { Array(10, 11, 12) }), "IS_ARRAY([10,11,12])" };
        yield return new object[] { Call("IS_BINARY", new BsonExpression[] { Constant(new BsonBinary(new byte[] { 255, 255, 255, 255 })) }), "IS_BINARY({\"$binary\":\"/////w==\"})" };
        yield return new object[] { Call("IS_OBJECTID", new BsonExpression[] { Constant(new BsonObjectId(new ObjectId("64de6507a2237f9d84596189"))) }), "IS_OBJECTID({\"$oid\":\"64de6507a2237f9d84596189\"})" };
        yield return new object[] { Call("IS_GUID", new BsonExpression[] { Constant(new BsonGuid(new Guid("cf9fc62e-6a10-4e0b-b597-bcd7c19dddf5"))) }), "IS_GUID({\"$guid\":\"cf9fc62e-6a10-4e0b-b597-bcd7c19dddf5\"})" };
        yield return new object[] { Call("IS_BOOLEAN", new BsonExpression[] { Constant(true) }), "IS_BOOLEAN(true)" };
        yield return new object[] { Call("IS_DATETIME", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16))) }), "IS_DATETIME({\"$date\":\"2000-10-16T03:00:00.0000000Z\"})" };
        yield return new object[] { Call("IS_MAXVALUE", new BsonExpression[] { Constant(BsonValue.MaxValue) }), "IS_MAXVALUE({\"$maxValue\":\"1\"})" };
        #endregion
        #endregion

        #region Date
        yield return new object[] { Call("YEAR", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16))) }), "YEAR({\"$date\":\"2000-10-16T03:00:00.0000000Z\"})" };
        yield return new object[] { Call("MONTH", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16))) }), "MONTH({\"$date\":\"2000-10-16T03:00:00.0000000Z\"})" };
        yield return new object[] { Call("DAY", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16))) }), "DAY({\"$date\":\"2000-10-16T03:00:00.0000000Z\"})" };
        yield return new object[] { Call("HOUR", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16, 12, 30, 56))) }), "HOUR({\"$date\":\"2000-10-16T15:30:56.0000000Z\"})" };
        yield return new object[] { Call("MINUTE", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16, 12, 30, 56))) }), "MINUTE({\"$date\":\"2000-10-16T15:30:56.0000000Z\"})" };
        yield return new object[] { Call("SECOND", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16, 12, 30, 56))) }), "SECOND({\"$date\":\"2000-10-16T15:30:56.0000000Z\"})" };
        yield return new object[] { Call("DATEADD", new BsonExpression[] { Constant("M"), Constant(1), Constant(new BsonDateTime(new DateTime(2000, 10, 16))) }), "DATEADD(\"M\",1,{\"$date\":\"2000-10-16T03:00:00.0000000Z\"})" };
        yield return new object[] { Call("DATEDIFF", new BsonExpression[] { Constant("M"), Constant(new BsonDateTime(new DateTime(2000, 10, 16))), Constant(new BsonDateTime(new DateTime(2001, 10, 16))) }), "DATEDIFF(\"M\",{\"$date\":\"2000-10-16T03:00:00.0000000Z\"},{\"$date\":\"2001-10-16T03:00:00.0000000Z\"})" };
        yield return new object[] { Call("TO_LOCAL", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16, 12, 00, 00, DateTimeKind.Utc))) }), "TO_LOCAL({\"$date\":\"2000-10-16T12:00:00.0000000Z\"})" };
        yield return new object[] { Call("TO_UTC", new BsonExpression[] { Constant(new BsonDateTime(new DateTime(2000, 10, 16, 9, 00, 00, DateTimeKind.Local))) }), "TO_UTC({\"$date\":\"2000-10-16T12:00:00.0000000Z\"})" };
        #endregion

        #region Math
        yield return new object[] { Call("ABS", new BsonExpression[] { Constant(-10) }), "ABS(-10)" };
        yield return new object[] { Call("ABS", new BsonExpression[] { Constant(-10.5) }), "ABS(-10.5)" };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2), Constant(1) }), "ROUND(2,1)" };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.4), Constant(0) }), "ROUND(2.4,0)" };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.5), Constant(0) }), "ROUND(2.5,0)" };
        yield return new object[] { Call("ROUND", new BsonExpression[] { Constant(2.6), Constant(0) }), "ROUND(2.6,0)" };
        yield return new object[] { Call("POW", new BsonExpression[] { Constant(2), Constant(3) }), "POW(2,3)" };
        #endregion

        #region Misc
        yield return new object[] { Call("JSON", new BsonExpression[] { Constant("{\"a\":1}") }), "JSON(\"{\\\"a\\\":1}\")" };
        yield return new object[] { Call("EXTEND", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["b"] = Constant(2) }), MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }) }), "EXTEND({b:2},{a:1})" };
        yield return new object[] { Call("KEYS", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) }), "KEYS({name:\"Maria\",age:18})" };
        yield return new object[] { Call("VALUES", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) }), "VALUES({name:\"Maria\",age:18})" };
        yield return new object[] { Call("OID_CREATIONTIME", new BsonExpression[] { Constant(2) }), "OID_CREATIONTIME(2)" };
        yield return new object[] { Call("COALESCE", new BsonExpression[] { Constant(10), Constant(20) }), "COALESCE(10,20)" };
        yield return new object[] { Call("COALESCE", new BsonExpression[] { Constant(BsonValue.Null), Constant(20) }), "COALESCE(null,20)" };
        yield return new object[] { Call("LENGTH", new BsonExpression[] { Constant("14LengthString") }), "LENGTH(\"14LengthString\")" };
        yield return new object[] { Call("LENGTH", new BsonExpression[] { Constant(new BsonBinary(new byte[] { 255, 255, 255 })) }), "LENGTH({\"$binary\":\"////\"})" };
        yield return new object[] { Call("LENGTH", new BsonExpression[] { Array(10, 11, 12, 13) }), "LENGTH([10,11,12,13])" };
        yield return new object[] { Call("LENGTH", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) }), "LENGTH({name:\"Maria\",age:18})" };
        yield return new object[] { Call("TOP", new BsonExpression[] { Array(10, 11, 12, 13), Constant(3) }), "TOP([10,11,12,13],3)" };
        yield return new object[] { Call("UNION", new BsonExpression[] { Array(10, 11, 12, 13), Array(14, 15, 16, 17) }), "UNION([10,11,12,13],[14,15,16,17])" };
        yield return new object[] { Call("EXCEPT", new BsonExpression[] { Array(10, 11, 12, 13), Array(12, 13, 14, 15) }), "EXCEPT([10,11,12,13],[12,13,14,15])" };
        yield return new object[] { Call("DISTINCT", new BsonExpression[] { Array(10, 10, 11, 12, 13) }), "DISTINCT([10,10,11,12,13])" };
        #endregion

        #region String
        yield return new object[] { Call("LOWER", new BsonExpression[] { Constant("LiteDB") }), "LOWER(\"LiteDB\")" };
        yield return new object[] { Call("UPPER", new BsonExpression[] { Constant("LiteDB") }), "UPPER(\"LiteDB\")" };
        yield return new object[] { Call("LTRIM", new BsonExpression[] { Constant("    LiteDB") }), "LTRIM(\"    LiteDB\")" };
        yield return new object[] { Call("RTRIM", new BsonExpression[] { Constant("LiteDB    ") }), "RTRIM(\"LiteDB    \")" };
        yield return new object[] { Call("TRIM", new BsonExpression[] { Constant("    LiteDB    ") }), "TRIM(\"    LiteDB    \")" };
        yield return new object[] { Call("INDEXOF", new BsonExpression[] { Constant("LiteDB"), Constant("D") }), "INDEXOF(\"LiteDB\",\"D\")" };
        yield return new object[] { Call("INDEXOF", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("D"), Constant(5) }), "INDEXOF(\"LiteDB-LiteDB\",\"D\",5)" };
        yield return new object[] { Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4) }), "SUBSTRING(\"LiteDB-LiteDB\",4)" };
        yield return new object[] { Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4), Constant(2) }), "SUBSTRING(\"LiteDB-LiteDB\",4,2)" };
        yield return new object[] { Call("REPLACE", new BsonExpression[] { Constant("LiteDB"), Constant("t"), Constant("v") }), "REPLACE(\"LiteDB\",\"t\",\"v\")" };
        yield return new object[] { Call("LPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), "LPAD(\"LiteDB\",10,\"-\")" };
        yield return new object[] { Call("RPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }), "RPAD(\"LiteDB\",10,\"-\")" };
        yield return new object[] { Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("-") }), "SPLIT(\"LiteDB-LiteDB\",\"-\")" };
        yield return new object[] { Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("(-)"), Constant(true) }), "SPLIT(\"LiteDB-LiteDB\",\"(-)\",true)" };
        yield return new object[] { Call("FORMAT", new BsonExpression[] { Constant(42), Constant("X") }), "FORMAT(42,\"X\")" };
        yield return new object[] { Call("JOIN", new BsonExpression[] { Array("LiteDB", "-LiteDB") }), "JOIN([\"LiteDB\",\"-LiteDB\"])" };
        yield return new object[] { Call("JOIN", new BsonExpression[] { Array("LiteDB", "LiteDB"), Constant("/") }), "JOIN([\"LiteDB\",\"LiteDB\"],\"/\")" };
        #endregion
        #endregion

        #region ConditionalExpressions

        yield return new object[] { Conditional(Constant(true), Constant(10), Constant(12)), "true?10:12" };
        yield return new object[] { Conditional(Constant(false), Constant(10), Constant(12)), "false?10:12" };
        yield return new object[] { Conditional(And(Constant(true), Constant(false)), Constant(10), Constant(12)), "true AND false?10:12" };
        yield return new object[] { Conditional(Or(Constant(true), Constant(false)), Constant(10), Constant(12)), "true OR false?10:12" };
        yield return new object[] { Conditional(Constant(true), Add(Constant(10), Constant(20)), Constant(12)), "true?10+20:12" };
        yield return new object[] { Conditional(Constant(false), Constant(10), Multiply(Constant(7), Constant(8))), "false?10:7*8" };

        #endregion

    }

    [Theory]
    [MemberData(nameof(Get_Expressions))]
    public void ToString_Theory(params object[] T)
    {
        var res = T[0].ToString();
        res.Should().Be(T[1] as string);
    }
}