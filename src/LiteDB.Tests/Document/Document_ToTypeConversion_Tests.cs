namespace LiteDB.Tests.Document;

public class Document_ToTypeConversion_Tests
{

    public static IEnumerable<object[]> Get_BsonValues()
    {
        yield return new object[] { new BsonInt32(10) };
        yield return new object[] { new BsonInt64(10) };
        yield return new object[] { new BsonDouble(10) };
        yield return new object[] { new BsonDecimal(10) };
        yield return new object[] { new BsonString("10") };
    }

    [Theory]
    [MemberData(nameof(Get_BsonValues))]
    public void Document_ToNumericTypeConversion(params object[] bsonValue)
    {
        //Arrange
        var value = bsonValue[0].As<BsonValue>();

        //Act + Assert
        value.ToInt32().Should().Be(10);
        value.ToInt64().Should().Be(10);
        value.ToDouble().Should().Be(10);
        value.ToDecimal().Should().Be(10);
    }

    public static IEnumerable<object[]> Get_BsonValuesToBool()
    {
        yield return new object[] { new BsonInt32(1), true };
        yield return new object[] { new BsonInt64(1), true };
        yield return new object[] { new BsonDouble(1), true };
        yield return new object[] { new BsonDecimal(1), true };
        yield return new object[] { new BsonString("true"), true };

        yield return new object[] { new BsonInt32(0), false };
        yield return new object[] { new BsonInt64(0), false };
        yield return new object[] { new BsonDouble(0), false };
        yield return new object[] { new BsonDecimal(0), false };
        yield return new object[] { new BsonString("false"), false };
    }

    [Theory]
    [MemberData(nameof(Get_BsonValuesToBool))]
    public void Document_ToBooleanConversion(params object[] bsonValue)
    {
        //Arrange
        var value = bsonValue[0].As<BsonValue>();
        var expected = bsonValue[1].As<bool>();

        //Act + Assert
        value.ToBoolean().Should().Be(expected);
    }
}
