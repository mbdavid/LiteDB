namespace LiteDB;

/// <summary>
/// Represent a single string token
/// </summary>
internal struct JsonToken
{
    public JsonToken(JsonTokenType tokenType, string value, long position)
    {
        this.Position = position;
        this.Value = value;
        this.Type = tokenType;
    }

    public JsonTokenType Type { get; private set; }
    public string Value { get; private set; }
    public long Position { get; private set; }

    /// <summary>
    /// Expect for one of the types (if not, throw UnexpectedToken)
    /// </summary>
    public JsonToken Expect(params JsonTokenType[] types)
    {
        foreach(var type in types)
        {
            if (this.Type == type)
            {
                return this;
            }
        }
        throw new Exception();
    }

    public bool Is(string value, bool ignoreCase = true)
    {
        return
            this.Type == JsonTokenType.Word &&
            value.Equals(this.Value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return $"{Value} ({Type})";
    }
}