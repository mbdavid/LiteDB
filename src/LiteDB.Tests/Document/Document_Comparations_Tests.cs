namespace Document;

public class Document_Comparations_Tests
{
    public static IEnumerable<object[]> Get_NumericPairs()
    {

        #region Same Type

        yield return new object[] { new BsonInt32(10), new BsonInt32(10) };
        yield return new object[] { new BsonInt64(10), new BsonInt64(10) };
        yield return new object[] { new BsonDouble(2.6), new BsonDouble(2.6) };
        yield return new object[] { new BsonDecimal(10), new BsonDecimal(10) };

        #endregion

        #region Different Type

        yield return new object[] { new BsonInt32(10), new BsonInt64(10) };
        yield return new object[] { new BsonInt32(10), new BsonDouble(10) };
        yield return new object[] { new BsonInt32(10), new BsonDecimal(10) };

        yield return new object[] { new BsonInt64(10), new BsonInt32(10) };
        yield return new object[] { new BsonInt64(10), new BsonDouble(10) };
        yield return new object[] { new BsonInt64(10), new BsonDecimal(10) };

        yield return new object[] { new BsonDouble(10), new BsonInt32(10) };
        yield return new object[] { new BsonDouble(10), new BsonDouble(10) };
        yield return new object[] { new BsonDouble(10), new BsonDecimal(10) };

        yield return new object[] { new BsonDecimal(10), new BsonInt32(10) };
        yield return new object[] { new BsonDecimal(10), new BsonInt64(10) };
        yield return new object[] { new BsonDecimal(10), new BsonDouble(10) };

        #endregion

    }

    [Theory]
    [MemberData(nameof(Get_NumericPairs))]
    public void Document_CompareNumeric_ShouldConvertToSameType(params object[] pair)
    {
        //Arrange
        var a = pair[0].As<BsonValue>();
        var b = pair[1].As<BsonValue>();

        //Act + Assert
        (a == b).Should().BeTrue();
        (a < b).Should().BeFalse();
        (a > b).Should().BeFalse();
        (a != b).Should().BeFalse();
        (a <= b).Should().BeTrue();
        (a >= b).Should().BeTrue();
    }

    public static IEnumerable<object[]> Get_StructurePairs()
    {
        var docLen1 = new BsonDocument() { ["_id"] = 10 };
        var docLen2 = new BsonDocument() { ["_id"] = 10, ["name"] = "Antonio" };
        var docLen3 = new BsonDocument() { ["_id"] = 10, ["name"] = "Antonio", ["age"] = 26 };

        var arrLen1 = new BsonArray() { 10 };
        var arrLen2 = new BsonArray() { 11, "string" };
        var arrLen3 = new BsonArray() { 12, "string", 2.6 };

        #region Same Type By Length

        yield return new object[] { docLen1, docLen2, false, true, false, true, true, false };
        yield return new object[] { docLen2, docLen2, true, false, false, false, true, true };
        yield return new object[] { docLen3, docLen2, false, false, true, true, false, true };
        yield return new object[] { arrLen1, arrLen2, false, true, false, true, true, false };
        yield return new object[] { arrLen2, arrLen2, true, false, false, false, true, true };
        yield return new object[] { arrLen3, arrLen2, false, false, true, true, false, true };

        #endregion

        #region Different Type

        yield return new object[] { docLen3, arrLen3, false, true, false, true, true, false };
        yield return new object[] { arrLen3, docLen3, false, false, true, true, false, true };

        #endregion

        #region Same Type By Value

        yield return new object[] { new BsonArray() { 1, 2, 3 }, new BsonArray() { 1, 2, 2 }, false, false, true, true, false, true };
        yield return new object[] { new BsonArray() { 1, 2, 3 }, new BsonArray() { 1, 2, 3 }, true, false, false, false, true, true };
        yield return new object[] { new BsonArray() { 1, 2, 3 }, new BsonArray() { 1, 2, 4 }, false, true, false, true, true, false };
        yield return new object[] { new BsonDocument() { ["a"] = 1, ["b"] = 2 }, new BsonDocument() { ["a"] = 1, ["b"] = 1 }, false, false, true, true, false, true };
        yield return new object[] { new BsonDocument() { ["a"] = 1, ["b"] = 2 }, new BsonDocument() { ["a"] = 1, ["b"] = 2 }, true, false, false, false, true, true };
        yield return new object[] { new BsonDocument() { ["a"] = 1, ["b"] = 2 }, new BsonDocument() { ["a"] = 1, ["b"] = 3 }, false, true, false, true, true, false };

        #endregion

    }

    [Theory]
    [MemberData(nameof(Get_StructurePairs))]
    public void Document_CompareStrucutures_ShouldConvertToSameType(params object[] pair)
    {
        //Arrange
        var a = pair[0].As<BsonValue>();
        var b = pair[1].As<BsonValue>();


        //Act + Assert
        (a == b).Should().Be(pair[2].As<bool>());
        (a < b).Should().Be(pair[3].As<bool>());
        (a > b).Should().Be(pair[4].As<bool>());
        (a != b).Should().Be(pair[5].As<bool>());
        (a <= b).Should().Be(pair[6].As<bool>());
        (a >= b).Should().Be(pair[7].As<bool>());
    }
}
