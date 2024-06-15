namespace LiteDB;

using System.Linq.Expressions;

/// <summary>
///     Class used to test in an Expression member expression is based on parameter `x => x.Name` or variable `x =>
///     externalVar`
/// </summary>
internal class ParameterExpressionVisitor : ExpressionVisitor
{
    public bool IsParameter { get; private set; }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        IsParameter = true;

        return base.VisitParameter(node);
    }

    public static bool Test(Expression node)
    {
        var instance = new ParameterExpressionVisitor();

        instance.Visit(node);

        return instance.IsParameter;
    }
}