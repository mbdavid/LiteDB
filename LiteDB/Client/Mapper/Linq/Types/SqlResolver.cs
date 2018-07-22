using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class SqlResolver : ITypeResolver
    {
        public string ResolveMethod(MethodInfo method)
        {
            var qtParams = method.GetParameters().Length;

            switch (method.Name)
            {
                // array index access
                case "Items": return qtParams == 1 ? "@0[*]" : "@0[@1]";

                // aggregate methods
                case "Count": return "COUNT(@0)";
                case "Sum": return "SUM(@0)";
                case "Avg": return "AVG(@0)";
                case "Min": return "MIN(@0)";
                case "Max": return "MAX(@0)";
                case "First": return "FIRST(@0)";
                case "Last": return "LAST(@0)";
            }

            throw new NotSupportedException($"Method {method.Name} are not supported when convert to BsonExpression.");
        }

        public bool HasSpecialMember => false;
        public string ResolveMember(MemberInfo member) => throw new NotImplementedException();
    }
}