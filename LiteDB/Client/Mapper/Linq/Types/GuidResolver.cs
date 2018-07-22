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
        public string ResolveMethod(MethodInfo method)
        {
            switch (method.Name)
            {
                case "NewGuid": return "GUID()";
                case "Parse": return "TO_GUID(@0)";
                case "TryParse": throw new NotSupportedException("There is no TryParse translate. Use Guid.Parse()");
            }

            return null;
        }

        public string ResolveMember(MemberInfo member)
        {
            switch (member.Name)
            {
                // static properties
                case "Empty": return "TO_GUID('00000000-0000-0000-0000-000000000000')";
            }

            return null;
        }

        public string ResolveCtor(ConstructorInfo ctor)
        {
            var pars = ctor.GetParameters();

            if (pars.Length == 1)
            {
                // string s
                if (pars[0].ParameterType == typeof(string))
                {
                    return "TO_GUID(@0)";
                }
            }

            return null;
        }
    }
}