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

                // convert methods
                case "ToArray":
                case "ToList": return "TO_ARRAY(@0)";

                // date extension
                case "DateDiff": return "DATEDIFF(@0, @1, @2)";
            }

            return null;
        }

        public string ResolveMember(MemberInfo member) => null;
        public string ResolveCtor(ConstructorInfo ctor) => null;
    }
}