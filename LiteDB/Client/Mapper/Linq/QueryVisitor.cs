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
    internal class QueryVisitor : ExpressionVisitor
    {
        private readonly BsonMapper _mapper;
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly BsonDocument _parameters = new BsonDocument();
        private int _paramIndex = 0;

        public QueryVisitor(BsonMapper mapper)
        {
            _mapper = mapper;
        }

        public BsonExpression Resolve(Expression expr)
        {
            this.Visit(expr);

            // remove last $ 
            var expression = _builder.Remove(_builder.Length - 1, 1).ToString();

            try
            {
                return BsonExpression.Create(expression, _parameters);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException($"Invalid LINQ expression: {expr.ToString()} - '{expression}'", ex);
            }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var op = this.GetOperator(node.NodeType);

            base.Visit(node.Left);

            _builder.Append(op);

            // when object is an native array, do not process right side
            if (op != "[*]")
            {
                base.Visit(node.Right);
            }

            return node;
        }

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

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var p = "p" + (_paramIndex++);

            _builder.AppendFormat("@" + p);

            _parameters[p] = _mapper.Serialize(node.Type, node.Value);

            return node;
        }

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

        protected override Expression VisitMember(MemberExpression node)
        {
            // for static member, Expression == null
            if (node.Expression != null)
            {
                base.VisitMember(node);
            }

            _builder.Append(this.ResolveName(node.Member));

            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _builder.Append("$");

            return base.VisitParameter(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Members == null || node.Members.Count == 0) throw new NotSupportedException("Expression not supported: " + node.ToString());

            //TODO: must check if new expression is new anonymous class

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

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var name = this.ResolveName(node.Method).Split('#');

            // write first part
            _builder.Append(name[0]);

            // for static method, Object == null
            this.Visit(node.Object ?? node.Arguments[0]);

            // if has #, write parameters and finish
            if (name.Length == 2)
            {
                foreach (var arg in node.Arguments.Skip(node.Object == null ? 1 : 0))
                {
                    _builder.Append(",");
                    this.Visit(arg);
                }

                _builder.Append(name[1]);
            }

            // when has 2 # is array access #[#]
            else if (name.Length == 3)
            {
                _builder.Append(name[1]);

                // if is simple array access will be converted into [*] all elements
                if (name[1] != "[*]")
                {
                    this.Visit(node.Arguments[1]);
                }

                _builder.Append(name[2]);
            }

            return node;
        }

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
                case ExpressionType.ArrayIndex: return "[*]";
            }
            throw new NotSupportedException("Operator not supported: " + nodeType.ToString());
        }

        private string ResolveName(MemberInfo member)
        {
            var isString = member.DeclaringType == typeof(string);
            var isDate = member.DeclaringType == typeof(DateTime);
            var isList = Reflection.IsList(member.DeclaringType);
            var isMethod = (member as MethodInfo) != null;
            var hasParams = isMethod && ((MethodInfo)member).GetParameters().Length > 1; // extensions always have first parameter

            var name = member.Name;

            var result =
                isList && name == "Length" ? "LENGTH(#)" :
                isList && name == "get_Item" ? "#[*]#" :
                isList && name == "ToArray" ? "" :
                isList && name == "ToList" ? "" :

                isList && !hasParams && name == "Count" ? "LENGTH(#)" :
                isList && !hasParams && name.StartsWith("First") ? "[0]" :
                isList && !hasParams && name.StartsWith("Single") ? "[0]" :

                // use ElementAt for index array navigation
                isList && hasParams && name == "ElementAt" ? "#[#]" :

                isDate && name == "Now" ? "DATE()" :
                isDate && name == "Year" ? "YEAR(#)" :
                isDate && name == "AddYears" ? "DATEADD('y',#)" :

                isString && name == "Length" ? "LENGTH(#)" :
                isString && name == "IndexOf" ? "INDEXOF(#)" :
                isString && name == "Contains" ? "INDEXOF(#) >= 0" :
                isString && name == "StartsWith" ? " LIKE #%" :
                //isString && name == "EndsWith" ? ".endsWith(#)" :
                isString && name == "PadLeft" ? "LPAD(#)" :
                isString && name == "PadRight" ? "RPAD(#)" :
                isString && name == "Substring" ? "SUBSTRING(#)" :
                isString && name == "Split" ? "SPLIT(#)" :
                isString && name == "Trim" ? "TRIM(#)" :
                isString && name == "TrimStart" ? "LTRIM(#)" :
                isString && name == "TrimEnd" ? "RTRIM(#)" :
                isString && name == "ToUpper" ? "UPPER(#)" :
                isString && name == "ToLower" ? "LOWER(#)" : null;

            if (isMethod && result == null) throw new NotSupportedException("Method not supported: " + name);

            if (result != null) return result;

            // get class entity from mapper
            var entity = _mapper.GetEntityMapper(member.DeclaringType);

            var field = entity.Members.FirstOrDefault(x => x.MemberName == name);

            if (field == null) throw new NotSupportedException($"Member {name} not found on BsonMapper.");

            return "." + field.FieldName;
        }
    }
}