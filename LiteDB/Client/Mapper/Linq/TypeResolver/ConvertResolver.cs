using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class ConvertResolver : ITypeResolver
    {
        public string ResolveMethod(MethodInfo method)
        {
            switch (method.Name)
            {
                case "ToInt32": return "TO_INT32(@0)";
                case "ToInt64": return "TO_INT64(@0)";
                case "ToDouble": return "TO_DOUBLE(@0)";
                case "ToDecimal": return "TO_DECIMAL(@0)";

                case "ToDateTime": return "TO_DATE(@0)";
                case "FromBase64String": return "TO_BINARY(@0)";
                case "ToBoolean": return "TO_BOOL(@0)";
                case "ToString": return "TO_STRING(@0)";
            }

            return null;
        }

        public string ResolveMember(MemberInfo member) => null;
        public string ResolveCtor(ConstructorInfo ctor) => null;
    }
}