using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class ResolveConvert : IResolveType
    {
        public bool HasSpecialMember => false;

        public string ResolveMethod(MethodInfo method)
        {
            /*
            Convert.ToInt32();
            Convert.ToDouble();
            Convert.ToDateTime();
            Convert.ToInt64();
            Convert.ToDecimal();
            Convert.ToString();
            Convert.FromBase64String();
            Convert.ToBoolean();
            */

            var qtParams = method.GetParameters().Length;

            switch (method.Name)
            {
                case "ToInt32": return "INT(@0)";
            }

            throw new NotSupportedException($"Method {method.Name} are not supported when convert to BsonExpression.");
        }

        public string ResolveMember(MemberInfo member)
        {
            throw new NotImplementedException();
        }
    }
}