using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class ResolveMath : IResolveType
    {
        public bool HasSpecialMember => false;

        public string ResolveMethod(MethodInfo method)
        {
            var qtParams = method.GetParameters().Length;

            switch (method.Name)
            {
                case "Abs": return "ABS(@0)";
                case "Pow": return "POW(@0, @1)";
                case "Round":
                    if (qtParams != 1) throw new ArgumentOutOfRangeException("Method Round need 2 arguments when convert to BsonExpression");
                    return "ROUND(@0, @1)";
            }

            throw new NotSupportedException($"Method {method.Name} are not supported when convert to BsonExpression.");
        }

        public string ResolveMember(MemberInfo member)
        {
            throw new NotImplementedException();
        }
    }
}