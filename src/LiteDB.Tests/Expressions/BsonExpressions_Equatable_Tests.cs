using static LiteDB.BsonExpression;

namespace LiteDB.Tests.Expressions;

public class BsonExpressions_Equatable_Tests
{


    public static IEnumerable<object[]> Get_Expressions()
    {

        yield return new object[] { Conditional(Constant(true), Constant(10), Constant(12)), Conditional(Constant(true), Constant(10), Constant(12)), true };
        yield return new object[] { Conditional(Constant(true), Constant(10), Constant(12)), Conditional(And(Constant(true), Constant(true)), Constant(10), Constant(12)), false };
        yield return new object[] { Conditional(Constant(true), Constant(10), Constant(12)), Conditional(Constant(false), Constant(10), Constant(12)), false };
        yield return new object[] { Conditional(Constant(true), Constant(10), Constant(12)), Conditional(Constant(true), Add(Constant(5), Constant(5)), Constant(12)), false };
        yield return new object[] { Conditional(Constant(true), Constant(10), Constant(12)), Conditional(Constant(true), Constant(11), Constant(12)), false };
        yield return new object[] { Conditional(Constant(true), Constant(10), Constant(12)), Conditional(Constant(true), Constant(10), Constant(13)), false };
    }

    [Theory]
    [MemberData(nameof(Get_Expressions))]
    public void Equals_Theory(params object[] T)
    {
        T[0].As<BsonExpression>().Equals(T[1].As<BsonExpression>()).Should().Be(T[2].As<bool>());
    }
}
