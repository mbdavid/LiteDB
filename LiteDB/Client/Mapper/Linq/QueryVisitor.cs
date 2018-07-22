using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class QueryVisitor : ExpressionVisitor
    {
        private static Dictionary<Type, IResolveType> _types = new Dictionary<Type, IResolveType>
        {
            [typeof(Convert)] = new ResolveConvert(),
            [typeof(DateTime)] = new ResolveDateTime(),
            [typeof(Enumerable)] = new ResolveEnumerable(),
            [typeof(Guid)] = new ResolveGuid(),
            [typeof(ObjectId)] = new ResolveObjectId(),
            [typeof(Sql)] = new ResolveSql(),
            [typeof(String)] = new ResolveString(),
            [typeof(Math)] = new ResolveMath()
        };

        private readonly BsonMapper _mapper;
        private readonly BsonDocument _parameters = new BsonDocument();

        private StringBuilder _builder = new StringBuilder();
        private int _paramIndex = 0;

        private string rootParameter = null;

        public QueryVisitor(BsonMapper mapper)
        {
            _mapper = mapper;
        }

        public BsonExpression Resolve(Expression expr)
        {
            this.Visit(expr);

            var expression = _builder.ToString();

            try
            {
                return BsonExpression.Create(expression, _parameters);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Invalid LINQ expression: {expr.ToString()} - '{expression}'", ex);
            }
        }

        /// <summary>
        /// Visit :: `x => x.Customer.Name`
        /// </summary>
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var l = base.VisitLambda(node);

            // remove last parameter $ (or @)
            _builder.Length--;

            return l;
        }

        /// <summary>
        /// Visit :: x => x.`Customer.Name`
        /// </summary>
        protected override Expression VisitMember(MemberExpression node)
        {
            var member = node.Member;

            // special types contains method access: string.Length, DateTime.Day, ...
            if (_types.TryGetValue(member.DeclaringType, out var type))
            {
                var pattern = type.ResolveMember(member).Split('#');

                _builder.Append(pattern[0]);

                if (node.Expression != null)
                {
                    base.VisitMember(node);
                    _builder.Append(pattern[1]);
                }
            }
            else
            {
                // for static member, Expression == null
                if (node.Expression != null)
                {
                    base.VisitMember(node);
                }

                var name = this.ResolveMember(member);

                _builder.Append(name);

            }

            return node;
        }

        /// <summary>
        /// Visit :: x => x.Customer.Name.`ToUpper()`
        /// </summary>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!_types.TryGetValue(node.Method.DeclaringType, out var type)) throw new NotSupportedException($"Type {node.Method.DeclaringType} not available to convert to BsonExpression");

            var pattern = type.ResolveMethod(node.Method);

            // retain current builder var to create another one
            var current = _builder;

            _builder = new StringBuilder();

            if (node.Object != null)
            {
                this.Visit(node.Object);
            }

            // get object expression string
            var objectExpr = _builder.ToString();
            var parameters = new Dictionary<int, string>();
            var index = 0;

            // now, get all parameter expressions strings
            foreach(var arg in node.Arguments)
            {
                _builder = new StringBuilder();
                this.Visit(arg);
                parameters[index++] = _builder.ToString();
            }

            // now, do replace for # to objet and @N as parameters
            var output = new StringBuilder();
            var tokenizer = new Tokenizer(pattern);

            // lets use tokenizer to parse this method pattern
            while(!tokenizer.EOF)
            {
                var token = tokenizer.ReadToken(false);

                if (token.Type == TokenType.Hashtag)
                {
                    output.Append(objectExpr);
                }
                else if(token.Type == TokenType.At)
                {
                    var i = Convert.ToInt32(tokenizer.ReadToken(false).Expect(TokenType.Int).Value);
                    output.Append(parameters[i]);
                }
                else
                {
                    output.Append(token.Type == TokenType.String ? "'" + token.Value + "'" : token.Value);
                }
            }

            // now restore current builder and append output
            _builder = current;
            _builder.Append(output.ToString());

            return node;
        }

        /// <summary>
        /// Visit :: x => x.Age + `10` (will create parameter:  `p0`, `p1`, ...)
        /// </summary>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            var p = "p" + (_paramIndex++);

            _builder.AppendFormat("@" + p);

            var value = _mapper.Serialize(node.Type, node.Value);

            _parameters[p] = value;

            return node;
        }

        /// <summary>
        /// Visit :: x => `!x.Active`
        /// </summary>
        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                _builder.Append("(");
                this.Visit(node.Operand);
                _builder.Append(") = false");
            }
            else
            {
                base.VisitUnary(node);
            }

            return node;
        }

        /// <summary>
        /// Visit :: x => `new { x.Id, x.Name }`
        /// </summary>
        protected override Expression VisitNew(NewExpression node)
        {
            // works only for anonymous classes
            if (node.Members == null || node.Members.Count == 0) throw new NotSupportedException("Expression not supported: " + node.ToString());

            _builder.Append("{ ");

            for (var i = 0; i < node.Members.Count; i++)
            {
                var member = node.Members[i];
                _builder.Append(i > 0 ? ", " : "");
                _builder.AppendFormat("'{0}': ", member.Name);
                this.Visit(node.Arguments[i]);
            }

            _builder.Append(" }");

            return node;
        }

        /// <summary>
        /// Visit :: x => `new int[] { 1, 2, 3 }`
        /// </summary>
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            _builder.Append("[ ");

            for (var i = 0; i < node.Expressions.Count; i++)
            {
                _builder.Append(i > 0 ? ", " : "");
                this.Visit(node.Expressions[i]);
            }

            _builder.Append(" ]");

            return node;
        }

        /// <summary>
        /// Visit :: x => `x`.Customer.Name
        /// </summary>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (rootParameter == null) rootParameter = node.Name;

            _builder.Append(node.Name == rootParameter ? "$" : "@");

            return base.VisitParameter(node);
        }

        /// <summary>
        /// Visit :: x => x.Id `+` 10
        /// </summary>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            var op = this.GetOperator(node.NodeType);

            base.Visit(node.Left);

            _builder.Append(op);

            base.Visit(node.Right);

            // when object is native array, access child using [n] is an Binary expression
            if (op == "[") _builder.Append("]");

            return node;
        }

        /// <summary>
        /// Visit :: x => `x.Id > 0 ? "ok" : "not-ok"`
        /// </summary>
        protected override Expression VisitConditional(ConditionalExpression node)
        {
            _builder.Append("IIF(");
            this.Visit(node.Test);
            _builder.Append(", ");
            this.Visit(node.IfTrue);
            _builder.Append(", ");
            this.Visit(node.IfFalse);
            _builder.Append(")");

            return node;
        }

        /// <summary>
        /// Get string operator from an Binary expression
        /// </summary>
        private string GetOperator(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add: return " + ";
                case ExpressionType.Multiply: return " * ";
                case ExpressionType.Subtract: return " - ";
                case ExpressionType.Divide: return " / ";
                case ExpressionType.Equal: return " = ";
                case ExpressionType.NotEqual: return " != ";
                case ExpressionType.GreaterThan: return " > ";
                case ExpressionType.GreaterThanOrEqual: return " >= ";
                case ExpressionType.LessThan: return " < ";
                case ExpressionType.LessThanOrEqual: return " <= ";
                case ExpressionType.AndAlso: return " AND ";
                case ExpressionType.OrElse: return " OR ";
                case ExpressionType.ArrayIndex: return "[";
            }

            throw new NotSupportedException("Operator not supported: " + nodeType.ToString());
        }

        /// <summary>
        /// Returns document field name for some type member
        /// </summary>
        private string ResolveMember(MemberInfo member)
        {
            var name = member.Name;

            // get class entity from mapper
            var entity = _mapper.GetEntityMapper(member.DeclaringType);

            var field = entity.Members.FirstOrDefault(x => x.MemberName == name);

            if (field == null) throw new NotSupportedException($"Member {name} not found on BsonMapper for type {member.DeclaringType}.");

            return "." + field.FieldName;
        }
    }
}