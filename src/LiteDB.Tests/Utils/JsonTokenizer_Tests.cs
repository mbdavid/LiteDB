namespace LiteDB.Tests.Internals.Engine;

public class JsonTokenizer_Tests
{
    [Fact]
    public void ReadToken_IntObject_()
    {
        #region Arrange

        var textReader = new StringReader("{ texto : 20}");
        var sut = new JsonTokenizer(textReader);

        #endregion

        #region Act

        List<JsonToken> tokens = new List<JsonToken>();

        while(!sut.EOF)
        {
            tokens.Add(sut.ReadToken());
        }

        #endregion

        #region Asserts

        //Token #0
        tokens[0].Type.Should().Be(JsonTokenType.OpenBrace);

        //Token #1
        tokens[1].Type.Should().Be(JsonTokenType.Word);
        tokens[1].Value.Should().Be("texto");

        //Token #2
        tokens[2].Type.Should().Be(JsonTokenType.Colon);

        //Token #3
        tokens[3].Type.Should().Be(JsonTokenType.Int);
        tokens[3].Value.Should().Be("20");

        //Token #4
        tokens[4].Type.Should().Be(JsonTokenType.CloseBrace);

        #endregion
    }

    [Fact]
    public void ReadToken_DoubleObject_()
    {
        #region Arrange

        var textReader = new StringReader("{ texto : 20.6}");
        var sut = new JsonTokenizer(textReader);

        #endregion

        #region Act

        List<JsonToken> tokens = new List<JsonToken>();

        while (!sut.EOF)
        {
            tokens.Add(sut.ReadToken());
        }

        #endregion

        #region Asserts

        //Token #0
        tokens[0].Type.Should().Be(JsonTokenType.OpenBrace);

        //Token #1
        tokens[1].Type.Should().Be(JsonTokenType.Word);
        tokens[1].Value.Should().Be("texto");

        //Token #2
        tokens[2].Type.Should().Be(JsonTokenType.Colon);

        //Token #3
        tokens[3].Type.Should().Be(JsonTokenType.Double);
        tokens[3].Value.Should().Be("20.6");

        //Token #4
        tokens[4].Type.Should().Be(JsonTokenType.CloseBrace);

        #endregion
    }

    [Fact]
    public void ReadToken_IntArray_()
    {
        #region Arrange

        var textReader = new StringReader("{ array : [10, 12, 14, 16]}");
        var sut = new JsonTokenizer(textReader);

        #endregion

        #region Act

        List<JsonToken> tokens = new List<JsonToken>();

        while (!sut.EOF)
        {
            tokens.Add(sut.ReadToken());
        }

        #endregion

        #region Asserts

        //Token #0
        tokens[0].Type.Should().Be(JsonTokenType.OpenBrace);

        //Token #1
        tokens[1].Type.Should().Be(JsonTokenType.Word);
        tokens[1].Value.Should().Be("array");

        //Token #2
        tokens[2].Type.Should().Be(JsonTokenType.Colon);

        //Token #3
        tokens[3].Type.Should().Be(JsonTokenType.OpenBracket);

        //Token #4
        tokens[4].Type.Should().Be(JsonTokenType.Int);
        tokens[4].Value.Should().Be("10");

        //Token #5
        tokens[5].Type.Should().Be(JsonTokenType.Comma);

        //Token #6
        tokens[6].Type.Should().Be(JsonTokenType.Int);
        tokens[6].Value.Should().Be("12");

        //Token #7
        tokens[7].Type.Should().Be(JsonTokenType.Comma);

        //Token #8
        tokens[8].Type.Should().Be(JsonTokenType.Int);
        tokens[8].Value.Should().Be("14");

        //Token #9
        tokens[9].Type.Should().Be(JsonTokenType.Comma);

        //Token #10
        tokens[10].Type.Should().Be(JsonTokenType.Int);
        tokens[10].Value.Should().Be("16");

        //Token #11
        tokens[11].Type.Should().Be(JsonTokenType.CloseBracket);

        //Token #12
        tokens[12].Type.Should().Be(JsonTokenType.CloseBrace);

        #endregion
    }

    [Fact]
    public void ReadToken_String_()
    {
        #region Arrange

        var textReader = new StringReader("\"String whith \\\" quotation\"");
        var sut = new JsonTokenizer(textReader);

        #endregion

        #region Act

        List<JsonToken> tokens = new List<JsonToken>();

        while (!sut.EOF)
        {
            tokens.Add(sut.ReadToken());
        }

        #endregion

        #region Asserts

        //Token #0
        tokens[0].Type.Should().Be(JsonTokenType.String);
        tokens[0].Value.Should().Be("String whith \" quotation");

        #endregion
    }

    [Fact]
    public void ReadToken_DotAfterSign_ShouldSeparateDotFromNumber()
    {
        #region Arrange

        var textReader = new StringReader("1-.5.5");
        var sut = new JsonTokenizer(textReader);

        #endregion

        #region Act

        List<JsonToken> tokens = new List<JsonToken>();

        while (!sut.EOF)
        {
            tokens.Add(sut.ReadToken());
        }

        #endregion

        #region Asserts

        //Token #0
        tokens[0].Type.Should().Be(JsonTokenType.Int);
        //Token #1
        tokens[1].Type.Should().Be(JsonTokenType.Minus);
        //Token #2
        tokens[2].Type.Should().Be(JsonTokenType.Period);
        //Token #3
        tokens[3].Type.Should().Be(JsonTokenType.Double);
        //Token #4
        tokens[3].Value.Should().Be("5.5");

        #endregion
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadToken_EatWhitespaceTrue_ShouldIgnoreWhiteSpaces(bool eatWhitespaces)
    {
        #region Arrange

        var textReader = new StringReader("   'String'   ");
        var sut = new JsonTokenizer(textReader);

        #endregion

        #region Act

        List<JsonToken> tokens = new List<JsonToken>();

        while (!sut.EOF)
        {
            tokens.Add(sut.ReadToken(eatWhitespaces));
        }

        #endregion

        #region Asserts

        //Token #0
        if (eatWhitespaces)
        {
            tokens[0].Type.Should().Be(JsonTokenType.String);
        }
        else
        {
            tokens[0].Type.Should().Be(JsonTokenType.Whitespace);
            tokens[1].Type.Should().Be(JsonTokenType.String);
            tokens[2].Type.Should().Be(JsonTokenType.Whitespace);
        }

        #endregion
    }
}