using static LiteDB.BsonExpression;

namespace LiteDB.Tests.Expressions;

public class BsonExpressions_TypeAndIsPredicate_Tests
{
    [Theory]
    [InlineData("21", BsonExpressionType.Constant, false)]
    [InlineData("2.6", BsonExpressionType.Constant, false)]
    [InlineData("'string'", BsonExpressionType.Constant, false)]
    [InlineData("2+1", BsonExpressionType.Add, false)]
    [InlineData("2-1", BsonExpressionType.Subtract, false)]
    [InlineData("2*1", BsonExpressionType.Multiply, false)]
    [InlineData("2/1", BsonExpressionType.Divide, false)]
    [InlineData("[1,2,3]", BsonExpressionType.Array, false)]
    [InlineData("1=1", BsonExpressionType.Equal, true)]
    [InlineData("2!=1", BsonExpressionType.NotEqual, true)]
    [InlineData("2>1", BsonExpressionType.GreaterThan, true)]
    [InlineData("2>=1", BsonExpressionType.GreaterThanOrEqual, true)]
    [InlineData("1<2", BsonExpressionType.LessThan, true)]
    [InlineData("1<=2", BsonExpressionType.LessThanOrEqual, true)]
    [InlineData("@p0", BsonExpressionType.Parameter, false)]
    [InlineData("UPPER(@p0)", BsonExpressionType.Call, false)]
    [InlineData("'LiteDB' LIKE 'L%'", BsonExpressionType.Like, true)]
    [InlineData("7 BETWEEN 4 AND 10", BsonExpressionType.Between, true)]
    [InlineData("7 IN [1,4,7]", BsonExpressionType.In, true)]
    [InlineData("true AND true", BsonExpressionType.And, false)]
    [InlineData("true OR false", BsonExpressionType.Or, false)]
    [InlineData("true?10:12", BsonExpressionType.Conditional, false)]
    [InlineData("arr=>@", BsonExpressionType.Map, false)]
   public void BsonExpressionTypeANDIsPredicate_Theory(string exp, BsonExpressionType type, bool isPredicate)
    {
        var expr = Create(exp);
        expr.Type.Should().Be(type);
        expr.IsPredicate.Should().Be(isPredicate);
    }

}