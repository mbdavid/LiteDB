using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LiteDB
{
    /// <summary>
    /// Class helper to create Queries based on Linq expressions
    /// </summary>
    internal class QueryVisitor<T>
    {
        private BsonMapper _mapper;
        private Type _type;

        public QueryVisitor(BsonMapper mapper)
        {
            _mapper = mapper;
            _type = typeof(T);
        }

        #region Visit Expression Switch Case

        public Query Visit(Expression<Func<T, bool>> predicate)
        {
            return VisitLambda(predicate as LambdaExpression) as Query;
        }

        private object VisitExpression(Expression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Lambda:
                    return VisitLambda(expr as LambdaExpression);
                case ExpressionType.MemberAccess:
                    return VisitMemberAccess(expr as MemberExpression);
                case ExpressionType.Constant:
                    return VisitConstant(expr as ConstantExpression);
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    return VisitBinary(expr as BinaryExpression);
                case ExpressionType.Not:
                case ExpressionType.Convert:
                    return VisitUnary(expr as UnaryExpression);
                case ExpressionType.Call:
                    return VisitMethodCall(expr as MethodCallExpression);
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    return VisitNewArray(expr as NewArrayExpression);
                default: throw new NotImplementedException("Expression not implemented: " + expr.ToString());
            }
        }

        #endregion

        /// <summary>
        /// Highest level is lambda expression: "x => x.Id == 1"
        /// </summary>
        private object VisitLambda(LambdaExpression lambda)
        {
            return VisitExpression(lambda.Body);
        }

        /// <summary>
        /// Returns an array of elements
        /// </summary>
        private object VisitNewArray(NewArrayExpression array)
        {
            return array.Expressions.Select(x => this.VisitExpression(x));
        }

        /// <summary>
        /// Binary expression are expresion with 2 sides, like ==, !=, >=, AND, OR
        /// </summary>
        private object VisitBinary(BinaryExpression bin)
        {
            var left = VisitExpression(bin.Left);
            var right = VisitExpression(bin.Right);

            switch (bin.NodeType)
            {
                case ExpressionType.Equal: return Query.EQ(left as string, right as BsonValue);
                case ExpressionType.NotEqual: return Query.Not(left as string, right as BsonValue);
                case ExpressionType.LessThan: return Query.LT(left as string, right as BsonValue);
                case ExpressionType.LessThanOrEqual: return Query.LTE(left as string, right as BsonValue);
                case ExpressionType.GreaterThan: return Query.GT(left as string, right as BsonValue);
                case ExpressionType.GreaterThanOrEqual: return Query.GTE(left as string, right as BsonValue);
                case ExpressionType.AndAlso: return Query.And(left as Query, right as Query);
                case ExpressionType.OrElse: return Query.Or(left as Query, right as Query);

                default: throw new NotImplementedException("Expression not implemented: " + bin.ToString());
            }
        }

        /// <summary>
        /// Not expression like "!(x.Name == "John")" or "!x.Active"
        /// Convert expression "x.Platform (enum => int)
        /// </summary>
        public object VisitUnary(UnaryExpression unary)
        {
            var operand = this.VisitExpression(unary.Operand);

            switch (unary.NodeType)
            {
                case ExpressionType.Not: return operand is string ? 
                        Query.Not(operand as string, true) : // !x.Active 
                        Query.Not(operand as Query); // !(x.Name == "John")
                case ExpressionType.Convert: return operand;
                default: throw new NotImplementedException("Expression not implemented: " + unary.ToString());
            }
        }

        /// <summary>
        /// Represent a method call from an object. Only few methods are implemented
        /// </summary>
        private object VisitMethodCall(MethodCallExpression method)
        {
            // support for IEnumerable list as parameter (Query.In)
            if (method.Method.DeclaringType.FullName == "System.Linq.Enumerable")
            {
                return VisitEnumerable(method);
            }

            var field = this.VisitExpression(method.Object) as string;
            var value = method.Arguments.Count >= 1 ? this.VisitExpression(method.Arguments[0]) as BsonValue : BsonValue.Null;

            switch (method.Method.Name)
            {
                case "StartsWith": return Query.StartsWith(field, value);
                case "Contains": return Query.Contains(field, value);
                case "Equals": return Query.EQ(field, value);
                default: throw new NotImplementedException("Expression not implemented: " + method.ToString());
            }
        }

        /// <summary>
        /// Member access are properties : "x.Name"
        /// </summary>
        private object VisitMemberAccess(MemberExpression member)
        {
            return this.GetField(member);
        }

        /// <summary>
        /// Represent a constant in expression. x => x.Id == 1
        /// </summary>
        private object VisitConstant(ConstantExpression constant)
        {
            return new BsonValue(constant.Value);
        }

        /// <summary>
        /// Support for (new int[] { 1, 2 }.Contains(x.Id))
        /// </summary>
        private object VisitEnumerable(MethodCallExpression method)
        {
            var values = this.VisitExpression(method.Arguments[0]);
            var field = this.VisitExpression(method.Arguments[1]);

            if (method.Method.Name == "Any" || method.Method.Name == "Contains")
            {
                return Query.In(field as string, (values as IEnumerable).Cast<BsonValue>());
            }

            throw new NotImplementedException("Expression not implemented: " + method.ToString());
        }

        /// <summary>
        /// Based on an expression, returns document field mapped from class Property.
        /// Support multi level dotted notation: x => x.Customer.Name
        /// </summary>
        public string GetField(Expression expr)
        {
            var property = expr.GetPath();
            var parts = property.Split('.');
            var fields = new string[parts.Length];
            var type = _type;
            var isdbref = false;

            // loop "first.second.last"
            for (var i = 0; i < parts.Length; i++)
            {
                var entity = _mapper.GetEntityMapper(type);
                var part = parts[i];
                var prop = entity.Props.Find(x => x.PropertyName == part);

                if (prop == null) throw LiteException.PropertyNotMapped(property);

                // if property is a IEnumerable, gets underlayer type (otherwise, gets PropertyType)
                type = prop.UnderlyingType;

                fields[i] = prop.FieldName;

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
    }
}