using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class Int32Resolver : ITypeResolver
    {
        public string ResolveMethod(MethodInfo method)
        {
            switch (method.Name)
            {
                // static methods
                case "Parse": return "TO_INT32(@0)";
            };

            throw new NotSupportedException($"Method {method.Name} are not supported when convert to BsonExpression.");
        }

        public bool HasSpecialMember => false;
        public string ResolveMember(MemberInfo member) => throw new NotImplementedException();
    }
}