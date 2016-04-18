using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using ParameterDictionary = System.Collections.Generic.Dictionary<System.Linq.Expressions.ParameterExpression,
                                                                  LiteDB.BsonValue>;

namespace LiteDB
{
    /// <summary>
    /// Class helper to create Queries based on Linq expressions
    /// </summary>
    internal class QueryVisitor<T>
    {
        private BsonMapper _mapper;
        private Type _type;
        private ParameterDictionary _parameters;

        public QueryVisitor(BsonMapper mapper)
        {
            _mapper = mapper;
            _type = typeof(T);
            _parameters = new ParameterDictionary();
        }

        public Query Visit(Expression predicate)
        {
            var lambda = predicate as LambdaExpression;
            return VisitExpression(lambda.Body);
        }

        private Query VisitExpression(Expression expr)
        {
            if (expr.NodeType == ExpressionType.Equal) // ==
            {
                var bin = expr as BinaryExpression;
                return new QueryEquals(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
            }
            else if (expr is MemberExpression && expr.Type == typeof(bool)) // x.Active
            {
                return Query.EQ(this.VisitMember(expr), new BsonValue(true));
            }
            else if (expr.NodeType == ExpressionType.NotEqual) // !=
            {
                var bin = expr as BinaryExpression;
                return Query.Not(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
            }
            else if (expr.NodeType == ExpressionType.Not) // !x.Active
            {
                var bin = expr as UnaryExpression;
                return Query.EQ(this.VisitMember(bin), new BsonValue(false));
            }
            else if (expr.NodeType == ExpressionType.LessThan) // <
            {
                var bin = expr as BinaryExpression;
                return Query.LT(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
            }
            else if (expr.NodeType == ExpressionType.LessThanOrEqual) // <=
            {
                var bin = expr as BinaryExpression;
                return Query.LTE(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
            }
            else if (expr.NodeType == ExpressionType.GreaterThan) // >
            {
                var bin = expr as BinaryExpression;
                return Query.GT(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
            }
            else if (expr.NodeType == ExpressionType.GreaterThanOrEqual) // >=
            {
                var bin = expr as BinaryExpression;
                return Query.GTE(this.VisitMember(bin.Left), this.VisitValue(bin.Right));
            }
            else if (expr is MethodCallExpression)
            {
                var met = expr as MethodCallExpression;
                var method = met.Method.Name;

                // StartsWith
                if (method == "StartsWith")
                {
                    var value = this.VisitValue(met.Arguments[0]);

                    return Query.StartsWith(this.VisitMember(met.Object), value);
                }
                // Contains
                else if (method == "Contains")
                {
                    var value = this.VisitValue(met.Arguments[0]);

                    return Query.Contains(this.VisitMember(met.Object), value);
                }
                // Equals
                else if (method == "Equals")
                {
                    var value = this.VisitValue(met.Arguments[0]);

                    return Query.EQ(this.VisitMember(met.Object), value);
                }
                // System.Linq.Enumerable methods
                else if (met.Method.DeclaringType.FullName == "System.Linq.Enumerable")
                {
                    return ParseEnumerableExpression(met);
                }
            }
            else if (expr is BinaryExpression && expr.NodeType == ExpressionType.AndAlso)
            {
                // AND
                var bin = expr as BinaryExpression;
                var left = this.VisitExpression(bin.Left);
                var right = this.VisitExpression(bin.Right);

                return Query.And(left, right);
            }
            else if (expr is BinaryExpression && expr.NodeType == ExpressionType.OrElse)
            {
                // OR
                var bin = expr as BinaryExpression;
                var left = this.VisitExpression(bin.Left);
                var right = this.VisitExpression(bin.Right);

                return Query.Or(left, right);
            }

            throw new NotImplementedException("Not implemented Linq expression");
        }

        private string VisitMember(Expression expr)
        {
            // quick and dirty solution to support x.Name.SubName
            // http://stackoverflow.com/questions/671968/retrieving-property-name-from-lambda-expression

            var str = expr.ToString(); // gives you: "o => o.Whatever"
            var firstDelim = str.IndexOf('.'); // make sure there is a beginning property indicator; the "." in "o.Whatever" -- this may not be necessary?

            var property = firstDelim < 0 ? str : str.Substring(firstDelim + 1).TrimEnd(')');

            return this.GetBsonField(property);
        }

        private BsonValue VisitValue(Expression expr)
        {
            // its a constant; Eg: "fixed string"
            if (expr is ConstantExpression)
            {
                var value = (expr as ConstantExpression);

                return _mapper.Serialize(value.Type, value.Value, 0);
            }
            else if (expr is MemberExpression && _parameters.Count > 0)
            {
                var mExpr = (MemberExpression)expr;
                var mValue = this.VisitValue(mExpr.Expression);
                var value = mValue.AsDocument[mExpr.Member.Name];
                return _mapper.Serialize(typeof(object), value, 0);
            }
            else if (expr is ParameterExpression)
            {
                BsonValue result;
                if (_parameters.TryGetValue((ParameterExpression)expr, out result))
                {
                    return result;
                }
            }

            // execute expression
            var objectMember = Expression.Convert(expr, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();

            return _mapper.Serialize(typeof(object), getter(), 0);
        }

        /// <summary>
        /// Get a Bson field from a simple Linq expression: x => x.CustomerName
        /// </summary>
        public string GetBsonField<TK, K>(Expression<Func<TK, K>> expr)
        {
            var member = expr.Body as MemberExpression;

            if (member == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", expr.ToString()));
            }

            return this.VisitMember(member);
        }

        /// <summary>
        /// Get a bson string field based on class PropertyInfo using BsonMapper class
        /// Support Name1.Name2 dotted notation
        /// </summary>
        private string GetBsonField(string property)
        {
            var parts = property.Split('.');
            var fields = new string[parts.Length];
            var type = _type;
            var isdbref = false;
            PropertyMapper prop;

            // loop "first.second.last"
            for (var i = 0; i < parts.Length; i++)
            {
                var map = _mapper.GetPropertyMapper(type);
                var part = parts[i];

                if (map.TryGetValue(part, out prop))
                {
                    type = prop.PropertyType;

                    fields[i] = prop.FieldName;

                    if(prop.FieldName == "_id" && isdbref)
                    {
                        isdbref = false;
                        fields[i] = "$id";
                    }

                    // if this property is DbRef, so if next property is _id, change to $id
                    if (prop.IsDbRef) isdbref = true;
                }
                else
                {
                    throw LiteException.PropertyNotMapped(property);
                }
            }

            return string.Join(".", fields);
        }

        private Query CreateAndQuery(ref Query[] queries, int startIndex = 0)
        {
            var length = queries.Length - startIndex;
            if (length == 0)
                return new QueryEmpty();

            if (length == 1)
                return queries[startIndex];
            else
                return Query.And(queries[startIndex], CreateOrQuery(ref queries, startIndex += 1));
        }

        private Query CreateOrQuery(ref Query[] queries, int startIndex = 0)
        {
            var length = queries.Length - startIndex;
            if (length == 0)
                return new QueryEmpty();

            if (length == 1)
                return queries[startIndex];
            else
                return Query.Or(queries[startIndex], CreateOrQuery(ref queries, startIndex += 1));
        }

        private Query ParseEnumerableExpression(MethodCallExpression expr)
        {
            if (expr.Method.DeclaringType.FullName != "System.Linq.Enumerable")
                throw new NotImplementedException("Cannot parse methods outside the System.Linq.Enumerable class.");

            var lambda = (LambdaExpression)expr.Arguments[1];
            var values = this.VisitValue(expr.Arguments[0]).AsArray;
            var queries = new Query[values.Count];

            for (var i = 0; i < queries.Length; i++)
            {
                _parameters[lambda.Parameters[0]] = values[i];
                queries[i] = this.VisitExpression(lambda.Body);
            }

            _parameters.Remove(lambda.Parameters[0]);

            if (expr.Method.Name == "Any")
                return CreateOrQuery(ref queries);
            else if (expr.Method.Name == "All")
                return CreateAndQuery(ref queries);

            throw new NotImplementedException("Not implemented System.Linq.Enumerable method");
        }
    }
}