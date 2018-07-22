using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class GuidResolver : ITypeResolver
    {
        public bool HasSpecialMember => true;

        public string ResolveMethod(MethodInfo method)
        {
            switch (method.Name)
            {
                case "NewGuid": return "GUID()";
                case "Parse": return "GUID(@0)";
                case "TryParse": throw new NotSupportedException("There is no TryParse translate. Use Guid.Parse()");
            }

            throw new NotSupportedException($"Method {method.Name} are not supported when convert to BsonExpression.");
        }

        public string ResolveMember(MemberInfo member)
        {
            switch (member.Name)
            {
                // static properties
                case "Empty": return "GUID('00000000-0000-0000-0000-000000000000')";
            }

            throw new NotSupportedException($"Member {member.Name} not supported when convert to BsonExpression.");
        }
    }
}