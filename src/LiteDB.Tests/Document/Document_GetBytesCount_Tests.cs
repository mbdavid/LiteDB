namespace LiteDB.Tests.Document;

public class Document_GetBytesCount_Tests
{

    public static IEnumerable<object[]> Get_BsonValues()
    {
        yield return new object[] { new BsonMinValue(), 0 };
        yield return new object[] { new BsonNull(), 0 };

        yield return new object[] { new BsonInt32(10), 4 };
        yield return new object[] { new BsonInt64(10), 8 };
        yield return new object[] { new BsonDouble(10), 8 };
        yield return new object[] { new BsonDecimal(10), 16 };

        #region String

        yield return new object[] { new BsonString("LiteDB"), 6 };
        yield return new object[] { new BsonString(""), 0 };
        yield return new object[] { new BsonString("çççç"), 8 };

        #endregion

        yield return new object[] { new BsonDocument() { ["_id"] = 10, ["name"] = "Antonio" }, 30 };
        yield return new object[] { new BsonArray() { 10, "Antonio" }, 21 };

        yield return new object[] { new BsonBinary(new byte[] { 255, 255, 255, 255 }), 4 };
        yield return new object[] { new BsonObjectId(new ObjectId()), 12 };
        yield return new object[] { new BsonGuid( new Guid("12345678-1234-1234-1234-123456789012")), 16 };
        yield return new object[] { new BsonDateTime(new DateTime(2000, 01, 01)), 8 };
        yield return new object[] { new BsonBoolean(true), 0 };

        yield return new object[] { new BsonMaxValue(), 0 };

    }

    [Theory]
    [MemberData(nameof(Get_BsonValues))]
    public void Document_GetBytesCount_Theory(params object[] T)
    {
        var value = T[0].As<BsonValue>();
        var bytesCount = T[1].As<int>();

        value.GetBytesCount().Should().Be(bytesCount);
    }
}
