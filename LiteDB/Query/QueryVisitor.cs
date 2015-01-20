using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Class helper to create Queries based on Linq expressions
    /// </summary>
    internal class QueryVisitor
    {
        public static Query Visit(Expression predicate)
        {
            var lambda = predicate as LambdaExpression;
            return VisitExpression(lambda.Body);
        }

        public static Query VisitExpression(Expression expr)
        {
            if (expr.NodeType == ExpressionType.Equal)
            {
                // ==
                var bin = expr as BinaryExpression;
                return new QueryEquals(VisitMember(bin.Left), VisitValue(bin.Right)); 
            }
            else if (expr.NodeType == ExpressionType.NotEqual)
            {
                // !=
                var bin = expr as BinaryExpression;
                return new QueryNot(VisitMember(bin.Left), VisitValue(bin.Right));
            }
            else if (expr.NodeType == ExpressionType.LessThan || expr.NodeType == ExpressionType.LessThanOrEqual)
            {
                // < <=
                var bin = expr as BinaryExpression;
                return new QueryLess(VisitMember(bin.Left), VisitValue(bin.Right), expr.NodeType == ExpressionType.LessThanOrEqual);
            }
            else if (expr.NodeType == ExpressionType.GreaterThan || expr.NodeType == ExpressionType.GreaterThanOrEqual)
            {
                // > >=
                var bin = expr as BinaryExpression;
                return new QueryGreater(VisitMember(bin.Left), VisitValue(bin.Right), expr.NodeType == ExpressionType.GreaterThanOrEqual);
            }
            else if (expr is MethodCallExpression)
            {
                var met = expr as MethodCallExpression;
                var method = met.Method.Name;

                // StartsWith
                if (method == "StartsWith")
                {
                    var field = (met.Object as MemberExpression).Member as PropertyInfo;
                    var value = VisitValue(met.Arguments[0]);

                    return new QueryStartsWith(field.Name, (string)value);
                }
                // Equals
                else if (method == "Equals")
                {
                    var field = (met.Object as MemberExpression).Member as PropertyInfo;
                    var value = VisitValue(met.Arguments[0]);

                    return new QueryEquals(field.Name, value);
                }
            }
            else if (expr is BinaryExpression && expr.NodeType == ExpressionType.AndAlso)
            {
                // AND
                var bin = expr as BinaryExpression;
                var left = VisitExpression(bin.Left);
                var right = VisitExpression(bin.Right);

                return new QueryAnd(left, right);
            }
            else if (expr is BinaryExpression && expr.NodeType == ExpressionType.OrElse)
            {
                // OR
                var bin = expr as BinaryExpression;
                var left = VisitExpression(bin.Left);
                var right = VisitExpression(bin.Right);

                return new QueryOr(left, right);
            }

            throw new NotImplementedException("Not implemented Linq expression");
        }

        private static string VisitMember(Expression expr)
        {
            var member = expr as MemberExpression;
            var propInfo = member.Member as PropertyInfo;

            if (propInfo == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", expr.ToString()));
            }

            return propInfo.Name;
        }

        public static object VisitValue(Expression expr)
        {
            // its a constant; Ex: "fixed string"
            if(expr is ConstantExpression)
            {
                return (expr as ConstantExpression).Value;
            }

            // execute expression
            var objectMember = Expression.Convert(expr, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();

            return getter();
        }

        public static PropertyInfo GetProperty<T, K>(Expression<Func<T, K>> expr)
        {
            var member = expr.Body as MemberExpression;

            if (member == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", expr.ToString()));
            }

            var propInfo = member.Member as PropertyInfo;

            if (propInfo == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a field, not a property.", expr.ToString()));
            }

            return propInfo;
        }
    }
}
