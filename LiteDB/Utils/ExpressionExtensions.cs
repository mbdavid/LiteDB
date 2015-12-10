using System;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    internal static class ExpressionExtensions
    {
        public static string GetPath<T, K>(this Expression<Func<T, K>> expr)
        {
            MemberExpression me;

            switch (expr.Body.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var ue = expr.Body as UnaryExpression;
                    me = ((ue != null) ? ue.Operand : null) as MemberExpression;
                    break;

                default:
                    me = expr.Body as MemberExpression;
                    break;
            }

            var sb = new StringBuilder();

            while (me != null)
            {
                var propertyName = me.Member.Name;
                var propertyType = me.Type;

                sb.Insert(0, propertyName + (sb.Length > 0 ? "." : ""));

                me = me.Expression as MemberExpression;
            }

            return sb.ToString();
        }
    }
}