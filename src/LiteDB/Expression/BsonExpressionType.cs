namespace LiteDB;

/// <summary>
/// Represent all types of BsonExpressions
/// </summary>
public enum BsonExpressionType : byte
{
    Constant = 1,

    ArrayIndex = 2,

    Array = 4,
    Document = 5,

    Parameter = 6,
    Call = 7,
    Root = 8,
    Current = 9,
    Path = 10,

    Modulo = 11,
    Add = 12,
    Subtract = 13,
    Multiply = 14,
    Divide = 15,

    Equal = 16,
    Like = 17,
    Between = 18,
    GreaterThan = 19,
    GreaterThanOrEqual = 20,
    LessThan = 21,
    LessThanOrEqual = 22,
    NotEqual = 23,
    In = 24,
    Contains = 25,

    Or = 30,
    And = 31,

    Inner = 32,

    Conditional = 40,
    Map = 41,
    Filter = 42,
    Sort = 43,

    Empty = 255
}

internal static class BsonExpressionExtensions
{
    /// <summary>
    /// Returns if BsonExpressionType is a predicate (return a boolean). AND/OR are not in this list
    /// </summary>
    internal static bool IsPredicate(this BsonExpressionType type)
    {
        return type == BsonExpressionType.Equal ||
               type == BsonExpressionType.Like ||
               type == BsonExpressionType.Between ||
               type == BsonExpressionType.GreaterThan ||
               type == BsonExpressionType.GreaterThanOrEqual ||
               type == BsonExpressionType.LessThan ||
               type == BsonExpressionType.LessThanOrEqual ||
               type == BsonExpressionType.NotEqual ||
               type == BsonExpressionType.In ||
               type == BsonExpressionType.Contains;

    }
}
