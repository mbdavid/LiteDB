using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class ObjectIdResolver : ITypeResolver
    {
        public bool HasSpecialMember => true;

        public string ResolveMethod(MethodInfo method)
        {

            throw new NotSupportedException($"Method {method.Name} are not supported when convert to BsonExpression.");
        }

        public string ResolveMember(MemberInfo member)
        {
            switch (member.Name)
            {
                // static properties
                case "Empty": return "OBJECTID('000000000000000000000000')";

                    // instance properties (not implemented in BsonExpression)
                    // case "CreationTime": return "...(#)";
            }

            throw new NotSupportedException($"Member {member.Name} not supported when convert to BsonExpression.");
        }
    }
}