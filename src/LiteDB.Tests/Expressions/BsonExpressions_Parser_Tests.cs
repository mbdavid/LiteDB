using static LiteDB.BsonExpression;

namespace LiteDB.Tests.Expressions;


public class BsonExpressions_Parser_Tests
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
        yield return new object[] { "10", Constant(10) };
        yield return new object[] { "2.6", Constant(2.6) };
        yield return new object[] { "true", Constant(true) };
        yield return new object[] { "\"LiteDB\"", Constant("LiteDB") };
        yield return new object[] { "[12,13,14]", Array(12, 13, 14) };
        yield return new object[] { "{a:1}", MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }) };
        yield return new object[] { "@LiteDB", Parameter("LiteDB") };
        yield return new object[] { "$.field", Path(Root, "field") };
        #endregion

        #region InterTypesInteraction
        yield return new object[] { "12+14", Add(Constant(12), Constant(14)) };
        yield return new object[] { "2.9+3", Add(Constant(2.9), Constant(3)) };
        yield return new object[] { "\"Lite\"+\"DB\"", Add(Constant("Lite"), Constant("DB")) };
        yield return new object[] { "12+\"string\"", Add(Constant(12), Constant("string")) };
        yield return new object[] { "{a:1}+\"string\"", Add(MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = "1" }), Constant("string")) };
        yield return new object[] { "1+\"string\"", Add(Constant(1), Constant("string")) };
        yield return new object[] { "[1,2]+3", Add(Array(1, 2), Constant(3)) };
        #endregion

        #region DocumentRelated
        yield return new object[] { "$.clients", Path(Root, "clients") };
        yield return new object[] { "$.doc.arr", Path(Path(Root, "doc"), "arr") };
        yield return new object[] { "@.name", Path(Current, "name") };
        yield return new object[] { "$.clients[age>=18]", Filter(Path(Root, "clients"), GreaterThanOrEqual(Path(Current, "age"), Constant(18))) };
        yield return new object[] { "$.clients=>@.name", Map(Path(Root, "clients"), Path(Current, "name")) };
        yield return new object[] { "$.arr[1]", ArrayIndex(Path(Root, "arr"), Constant(1)) };
        #endregion

        #region BinaryExpressions
        yield return new object[] { "'LiteDB' LIKE 'L%'", Like(Constant("LiteDB"), Constant("L%")) };
        yield return new object[] { "1+2", Add(Constant(1), Constant(2)) };
        yield return new object[] { "1-2", Subtract(Constant(1), Constant(2)) };
        yield return new object[] { "1*2", Multiply(Constant(1), Constant(2)) };
        yield return new object[] { "1/2", Divide(Constant(1), Constant(2)) };
        yield return new object[] { "1%2", Modulo(Constant(1), Constant(2)) };
        yield return new object[] { "1=2", Equal(Constant(1), Constant(2)) };
        yield return new object[] { "1!=2", NotEqual(Constant(1), Constant(2)) };
        yield return new object[] { "1>2", GreaterThan(Constant(1), Constant(2)) };
        yield return new object[] { "1>=2", GreaterThanOrEqual(Constant(1), Constant(2)) };
        yield return new object[] { "1<2", LessThan(Constant(1), Constant(2)) };
        yield return new object[] { "1<=2", LessThanOrEqual(Constant(1), Constant(2)) };
        yield return new object[] { "[1,2] CONTAINS 3", Contains(Array(1, 2), Constant(3)) };
        yield return new object[] { "1 BETWEEN 2 AND 3", Between(Constant(1), Array(2, 3)) };
        yield return new object[] { "'LiteDB' LIKE 'L%'", Like(Constant("LiteDB"), Constant("L%")) };
        yield return new object[] { "1 IN [2,3]", In(Constant(1), Array(2, 3)) };
        yield return new object[] { "true OR false", Or(Constant(true), Constant(false)) };
        yield return new object[] { "true AND false", And(Constant(true), Constant(false)) };
        #endregion

        #region CallMethods
        #region DataTypes
        #region NEW_INSTANCE
        yield return new object[] { "MINVALUE()" , Call("MINVALUE", new BsonExpression[] { })   };
        yield return new object[] { "OBJECTID()" , Call("OBJECTID", new BsonExpression[] { })   };
        yield return new object[] { "GUID()"     , Call("GUID", new BsonExpression[] { })       };
        yield return new object[] { "NOW()"      , Call("NOW", new BsonExpression[] { })        };
        yield return new object[] { "NOW_UTC()"  , Call("NOW_UTC", new BsonExpression[] { })    };
        yield return new object[] { "TODAY()"    , Call("TODAY", new BsonExpression[] { })      };
        yield return new object[] { "MAXVALUE()" , Call("MAXVALUE", new BsonExpression[] { })   };
        #endregion

        #region DATATYPE
        yield return new object[] { "INT32(2.4)"                                                , Call("INT32", new BsonExpression[] { Constant(2.4) })                                                    };
        yield return new object[] { "INT64(2)"                                                  , Call("INT64", new BsonExpression[] { Constant(2) })                                                      };
        yield return new object[] { "DOUBLE(2)"                                                 , Call("DOUBLE", new BsonExpression[] { Constant(2) })                                                     };
        yield return new object[] { "DECIMAL(2)"                                                , Call("DECIMAL", new BsonExpression[] { Constant(2) })                                                    };
        yield return new object[] { "STRING(2)"                                                 , Call("STRING", new BsonExpression[] { Constant(2) })                                                     };
        yield return new object[] { "BINARY(\"11111111\")"                                      , Call("BINARY", new BsonExpression[] { Constant(new BsonString("11111111")) })                            };
        yield return new object[] { "OBJECTID(2)"                                               , Call("OBJECTID", new BsonExpression[] { Constant(new BsonInt32(2)) })                                    };
        yield return new object[] { "GUID(\"cf9fc62e-6a10-4e0b-b597-bcd7c19dddf5\")"            , Call("GUID", new BsonExpression[] { Constant("cf9fc62e-6a10-4e0b-b597-bcd7c19dddf5") })                  };
        yield return new object[] { "BOOLEAN(true)"                                             , Call("BOOLEAN", new BsonExpression[] { Constant(true) })                                                 };
        yield return new object[] { "DATETIME(2000,10,16)"                                      , Call("DATETIME", new BsonExpression[] { Constant(2000), Constant(10), Constant(16) })                    };
        yield return new object[] { "DATETIME_UTC(2000,10,16)"                                  , Call("DATETIME_UTC", new BsonExpression[] { Constant(2000), Constant(10), Constant(16) })                };
        #endregion

        #region IS_DATETYPE
        yield return new object[] { "IS_NULL(null)"                                                   , Call("IS_NULL", new BsonExpression[] { Constant(BsonValue.Null) })                                                                                                        };
        yield return new object[] { "IS_INT32(2)"                                                     , Call("IS_INT32", new BsonExpression[] { Constant(2) })                                                                                                                    };
        yield return new object[] { "IS_DOUBLE(2.6)"                                                  , Call("IS_DOUBLE", new BsonExpression[] { Constant(2.6) })                                                                                                                 };
        yield return new object[] { "IS_NUMBER(2)"                                                    , Call("IS_NUMBER", new BsonExpression[] { Constant(2) })                                                                                                                   };
        yield return new object[] { "IS_STRING(\"string\")"                                           , Call("IS_STRING", new BsonExpression[] { Constant("string") })                                                                                                            };
        yield return new object[] { "IS_DOCUMENT({name:\"Maria\",age:18})"                            , Call("IS_DOCUMENT", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) })               };
        yield return new object[] { "IS_ARRAY([10,11,12])"                                            , Call("IS_ARRAY", new BsonExpression[] { Array(10, 11, 12) })                                                                                                              };
        yield return new object[] { "IS_BOOLEAN(true)"                                                , Call("IS_BOOLEAN", new BsonExpression[] { Constant(true) })                                                                                                               };
        #endregion
        #endregion

        #region Math
        yield return new object[] { "ABS(-10)", Call("ABS", new BsonExpression[] { Constant(-10) }) };
        yield return new object[] { "ABS(-10.5)", Call("ABS", new BsonExpression[] { Constant(-10.5) }) };
        yield return new object[] { "ROUND(2,1)", Call("ROUND", new BsonExpression[] { Constant(2), Constant(1) }) };
        yield return new object[] { "ROUND(2.4,0)", Call("ROUND", new BsonExpression[] { Constant(2.4), Constant(0) }) };
        yield return new object[] { "ROUND(2.5,0)", Call("ROUND", new BsonExpression[] { Constant(2.5), Constant(0) }) };
        yield return new object[] { "ROUND(2.6,0)", Call("ROUND", new BsonExpression[] { Constant(2.6), Constant(0) }) };
        yield return new object[] { "POW(2,3)", Call("POW", new BsonExpression[] { Constant(2), Constant(3) }) };
        #endregion

        #region Misc
        yield return new object[] { "JSON(\"{\\\"a\\\":1}\")"              , Call("JSON", new BsonExpression[] { Constant("{\"a\":1}") })                                                                                                                                       };
        yield return new object[] { "EXTEND({b:2},{a:1})"                  , Call("EXTEND", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["b"] = Constant(2) }), MakeDocument(new Dictionary<string, BsonExpression> { ["a"] = Constant(1) }) })};
        yield return new object[] { "KEYS({name:\"Maria\",age:18})"        , Call("KEYS", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) })                                               };
        yield return new object[] { "VALUES({name:\"Maria\",age:18})"      , Call("VALUES", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) })                                             };
        yield return new object[] { "OID_CREATIONTIME(2)"                  , Call("OID_CREATIONTIME", new BsonExpression[] { Constant(2) })                                                                                                                                     };
        yield return new object[] { "COALESCE(10,20)"                      , Call("COALESCE", new BsonExpression[] { Constant(10), Constant(20) })                                                                                                                              };
        yield return new object[] { "COALESCE(null,20)"                    , Call("COALESCE", new BsonExpression[] { Constant(BsonValue.Null), Constant(20) })                                                                                                                  };
        yield return new object[] { "LENGTH(\"14LengthString\")"           , Call("LENGTH", new BsonExpression[] { Constant("14LengthString") })                                                                                                                                };
        yield return new object[] { "LENGTH([10,11,12,13])"                , Call("LENGTH", new BsonExpression[] { Array(10, 11, 12, 13) })                                                                                                                                     };
        yield return new object[] { "LENGTH({name:\"Maria\",age:18})"      , Call("LENGTH", new BsonExpression[] { MakeDocument(new Dictionary<string, BsonExpression> { ["name"] = Constant("Maria"), ["age"] = Constant(18) }) })                                             };
        yield return new object[] { "TOP([10,11,12,13],3)"                 , Call("TOP", new BsonExpression[] { Array(10, 11, 12, 13), Constant(3) })                                                                                                                           };
        yield return new object[] { "UNION([10,11,12,13],[14,15,16,17])"   , Call("UNION", new BsonExpression[] { Array(10, 11, 12, 13), Array(14, 15, 16, 17) })                                                                                                               };
        yield return new object[] { "EXCEPT([10,11,12,13],[12,13,14,15])"  , Call("EXCEPT", new BsonExpression[] { Array(10, 11, 12, 13), Array(12, 13, 14, 15) })                                                                                                              };
        yield return new object[] { "DISTINCT([10,10,11,12,13])"           , Call("DISTINCT", new BsonExpression[] { Array(10, 10, 11, 12, 13) })                                                                                                                               };
        #endregion

        #region String
        yield return new object[] { "LOWER(\"LiteDB\")", Call("LOWER", new BsonExpression[] { Constant("LiteDB") }) };
        yield return new object[] { "UPPER(\"LiteDB\")", Call("UPPER", new BsonExpression[] { Constant("LiteDB") }) };
        yield return new object[] { "LTRIM(\"    LiteDB\")", Call("LTRIM", new BsonExpression[] { Constant("    LiteDB") }) };
        yield return new object[] { "RTRIM(\"LiteDB    \")", Call("RTRIM", new BsonExpression[] { Constant("LiteDB    ") }) };
        yield return new object[] { "TRIM(\"    LiteDB    \")", Call("TRIM", new BsonExpression[] { Constant("    LiteDB    ") }) };
        yield return new object[] { "INDEXOF(\"LiteDB\",\"D\")", Call("INDEXOF", new BsonExpression[] { Constant("LiteDB"), Constant("D") }) };
        yield return new object[] { "INDEXOF(\"LiteDB-LiteDB\",\"D\",5)", Call("INDEXOF", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("D"), Constant(5) }) };
        yield return new object[] { "SUBSTRING(\"LiteDB-LiteDB\",4)", Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4) }) };
        yield return new object[] { "SUBSTRING(\"LiteDB-LiteDB\",4,2)", Call("SUBSTRING", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant(4), Constant(2) }) };
        yield return new object[] { "REPLACE(\"LiteDB\",\"t\",\"v\")", Call("REPLACE", new BsonExpression[] { Constant("LiteDB"), Constant("t"), Constant("v") }) };
        yield return new object[] { "LPAD(\"LiteDB\",10,\"-\")", Call("LPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }) };
        yield return new object[] { "RPAD(\"LiteDB\",10,\"-\")", Call("RPAD", new BsonExpression[] { Constant("LiteDB"), Constant(10), Constant("-") }) };
        yield return new object[] { "SPLIT(\"LiteDB-LiteDB\",\"-\")", Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("-") }) };
        yield return new object[] { "SPLIT(\"LiteDB-LiteDB\",\"(-)\",true)", Call("SPLIT", new BsonExpression[] { Constant("LiteDB-LiteDB"), Constant("(-)"), Constant(true) }) };
        yield return new object[] { "FORMAT(42,\"X\")", Call("FORMAT", new BsonExpression[] { Constant(42), Constant("X") }) };
        yield return new object[] { "JOIN([\"LiteDB\",\"-LiteDB\"])", Call("JOIN", new BsonExpression[] { Array("LiteDB", "-LiteDB") }) };
        yield return new object[] { "JOIN([\"LiteDB\",\"LiteDB\"],\"/\")", Call("JOIN", new BsonExpression[] { Array("LiteDB", "LiteDB"), Constant("/") }) };
        #endregion
        #endregion

        #region ConditionalExpressions

        yield return new object[] { "true ? 10 : 12", Conditional(Constant(true), Constant(10), Constant(12)) };
        yield return new object[] { "false ? 10 : 12", Conditional(Constant(false), Constant(10), Constant(12)) };
        yield return new object[] { "true AND false ? 10 : 12", Conditional(And(Constant(true), Constant(false)), Constant(10), Constant(12)) };
        yield return new object[] { "true OR false ? 10 : 12", Conditional(Or(Constant(true), Constant(false)), Constant(10), Constant(12)) };
        yield return new object[] { "true ? 10+20 : 12", Conditional(Constant(true), Add(Constant(10), Constant(20)), Constant(12)) };
        yield return new object[] { "false ? 10 : 7*8", Conditional(Constant(false), Constant(10), Multiply(Constant(7), Constant(8))) };

        #endregion
    }

    [Theory]
    [MemberData(nameof(Get_Expressions))]
    public void Create_Theory(params object[] T)
    {
        var test = Create(T[0] as string);
        test.Should().Be(T[1] as BsonExpression);
    }

    [Theory]
    [InlineData("1 BETWEEN 1")]
    [InlineData("{a:1 b:1}")]
    [InlineData("[1,2 3]")]
    [InlineData("true OR (x>1")]
    [InlineData("UPPER('abc'")]
    [InlineData("INDEXOF('abc''b')")]
    public void Create_MisTyped_ShouldThrowException(string expr)
    {
        Assert.Throws<LiteException>(() => Create(expr));
    }
}