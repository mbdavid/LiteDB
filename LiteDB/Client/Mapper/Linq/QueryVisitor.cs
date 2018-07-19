using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using ParameterDictionary = System.Collections.Generic.Dictionary<System.Linq.Expressions.ParameterExpression, LiteDB.BsonValue>;

namespace LiteDB
{
    /// <summary>
    /// Class helper to create Queries based on Linq expressions
    /// </summary>
    public class QueryVisitor<T>
    {
        private readonly BsonMapper _mapper;
        private readonly Type _type;
        private readonly ParameterDictionary _parameters = new ParameterDictionary();
        private ParameterExpression _param = null;

        public QueryVisitor(BsonMapper mapper)
        {
            _mapper = mapper;
            _type = typeof(T);
        }

        public BsonExpression VisitExpression(Expression predicate)
        {
            var lambda = predicate as LambdaExpression;

            var sb = new StringBuilder();
            var parameters = new BsonDocument();
            var paramIndex = 0;

            _param = lambda.Parameters[0];

            this.VisitExpression(sb, parameters, paramIndex, lambda.Body);

            return BsonExpression.Create(sb.ToString(), parameters);
        }

        public BsonExpression VisitPath(Expression predicate)
        {
            var lambda = predicate as LambdaExpression;

            var sb = new StringBuilder();
            var parameters = new BsonDocument();
            var paramIndex = 0;

            _param = lambda.Parameters[0];

            this.VisitExpression(sb, parameters, paramIndex, lambda.Body);

            return BsonExpression.Create(sb.ToString(), parameters);
        }

        private void VisitExpression(StringBuilder sb, BsonDocument parameters, int paramIndex, Expression expr, string prefix = null)
        {
            // Single: x.Active
            if (expr is MemberExpression && expr.Type == typeof(bool))
            {
                sb.Append(this.VisitPath(expr, prefix) + " = true");
            }
        }

        private string VisitPath(Expression expr, string prefix = "", bool showArrayItems = false)
        {
            var property = prefix + expr.GetPath();
            var parts = property.Split('.');
            var fields = new string[parts.Length];
            var type = _type;
            var isdbref = false;

            // loop "first.second.last"
            for (var i = 0; i < parts.Length; i++)
            {
                var entity = _mapper.GetEntityMapper(type);
                var part = parts[i];
                var prop = entity.Members.Find(x => x.MemberName == part);

                if (prop == null) throw new NotSupportedException(property + " not mapped in " + type.Name);

                // if property is an IEnumerable, gets underlying type (otherwise, gets PropertyType)
                type = prop.UnderlyingType;

                fields[i] = prop.FieldName;

                if (showArrayItems && prop.IsList)
                {
                    fields[i] += "[*]";
                }

                if (prop.FieldName == "_id" && isdbref)
                {
                    isdbref = false;
                    fields[i] = "$id";
                }

                // if this property is DbRef, so if next property is _id, change to $id
                if (prop.IsDbRef) isdbref = true;
            }

            return string.Join(".", fields);
        }

        private BsonValue VisitValue(Expression expr, Expression left)
        {
            // check if left side is an enum and convert to string before return
            BsonValue convert(Type type, object value)
            {
                var enumType = (left as UnaryExpression)?.Operand.Type;

                if (enumType != null && enumType.GetTypeInfo().IsEnum)
                {
                    var str = Enum.GetName(enumType, value);
                    return _mapper.Serialize(typeof(string), str, 0);
                }

                return _mapper.Serialize(type, value, 0);
            }

            // its a constant; Eg: "fixed string"
            if (expr is ConstantExpression)
            {
                var value = (expr as ConstantExpression);

                return convert(value.Type, value.Value);
            }
            else if (expr is MemberExpression && _parameters.Count > 0)
            {
                var mExpr = (MemberExpression)expr;
                var mValue = this.VisitValue(mExpr.Expression, left);
                var value = mValue[mExpr.Member.Name];

                return convert(typeof(object), value);
            }
            else if (expr is ParameterExpression)
            {
                if (_parameters.TryGetValue((ParameterExpression)expr, out BsonValue result))
                {
                    return result;
                }
            }

            // execute expression
            var objectMember = Expression.Convert(expr, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();

            return convert(typeof(object), getter());
        }
    }
}