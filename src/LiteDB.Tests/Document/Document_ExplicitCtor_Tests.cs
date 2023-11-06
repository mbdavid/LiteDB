namespace LiteDB.Tests.Document;

public class Document_ExplicitCtor_Tests
{

    [Fact]
    public void Document_BsonMinValueCtor()
    {
        var value = new BsonMinValue();
        value.IsNumber.Should().BeFalse();
        value.IsMinValue.Should().BeTrue();
        value.GetBytesCount().Should().Be(0);
    }

    [Fact]
    public void Document_BsonNullCtor()
    {
        var value = new BsonNull();
        value.IsNumber.Should().BeFalse();
        value.IsNull.Should().BeTrue();
        value.GetBytesCount().Should().Be(0);
    }

    #region Numeric

    [Fact]
    public void Document_BsonInt32Ctor()
    {
        var value = new BsonInt32(10);
        value.IsNumber.Should().BeTrue();
        value.IsInt32.Should().BeTrue();
        value.GetBytesCount().Should().Be(4);
    }

    [Fact]
    public void Document_BsonInt64Ctor()
    {
        var value = new BsonInt64(10);
        value.IsNumber.Should().BeTrue();
        value.IsInt64.Should().BeTrue();
        value.GetBytesCount().Should().Be(8);
    }

    [Fact]
    public void Document_BsonDoubleCtor()
    {
        var value = new BsonDouble(10);
        value.IsNumber.Should().BeTrue();
        value.IsDouble.Should().BeTrue();
        value.GetBytesCount().Should().Be(8);
    }

    [Fact]
    public void Document_BsonDecimalCtor()
    {
        var value = new BsonDecimal(10);
        value.IsNumber.Should().BeTrue();
        value.IsDecimal.Should().BeTrue();
        value.GetBytesCount().Should().Be(16);
    }

    #endregion

    [Fact]
    public void Document_BsonStringCtor()
    {
        var value = new BsonString("LiteDB");
        value.IsNumber.Should().BeFalse();
        value.IsString.Should().BeTrue();
        value.GetBytesCount().Should().Be(6);
    }

    #region Structures

    [Fact]
    public void Document_BsonDocumentCtor()
    {
        var value = new BsonDocument() { ["_id"] = 10 };
        value.IsNumber.Should().BeFalse();
        value.IsDocument.Should().BeTrue();
        value.GetBytesCount().Should().Be(13);
    }

    [Fact]
    public void Document_BsonArrayCtor()
    {
        var value = new BsonArray() { 10, 2.6, "string"};
        value.IsNumber.Should().BeFalse();
        value.IsArray.Should().BeTrue();
        value.GetBytesCount().Should().Be(29);
    }

    #endregion

    [Fact]
    public void Document_BsonBinaryCtor()
    {
        var value = new BsonBinary(new byte[] { 255, 255, 255, 255});
        value.IsNumber.Should().BeFalse();
        value.IsBinary.Should().BeTrue();
        value.GetBytesCount().Should().Be(4);
    }

    [Fact]
    public void Document_BsonObjectIdCtor()
    {
        var value = new BsonObjectId(new ObjectId());
        value.IsNumber.Should().BeFalse();
        value.IsObjectId.Should().BeTrue();
        value.GetBytesCount().Should().Be(12);
    }

    [Fact]
    public void Document_BsonGuidCtor()
    {
        var value = new BsonGuid(new Guid("12345678-1234-1234-1234-123456789012"));
        value.IsNumber.Should().BeFalse();
        value.IsGuid.Should().BeTrue();
        value.GetBytesCount().Should().Be(16);
    }

    [Fact]
    public void Document_BsonDateTimeCtor()
    {
        var value = new BsonDateTime(new DateTime(2000, 01, 01));
        value.IsNumber.Should().BeFalse();
        value.IsDateTime.Should().BeTrue();
        value.GetBytesCount().Should().Be(8);
    }

    [Fact]
    public void Document_BsonBooleanCtor()
    {
        var value = new BsonBoolean(true);
        value.IsNumber.Should().BeFalse();
        value.IsBoolean.Should().BeTrue();
        value.GetBytesCount().Should().Be(0);
    }

    [Fact]
    public void Document_BsonMaxValueCtor()
    {
        var value = new BsonMaxValue();
        value.IsNumber.Should().BeFalse();
        value.IsMaxValue.Should().BeTrue();
        value.GetBytesCount().Should().Be(0);
    }
}
