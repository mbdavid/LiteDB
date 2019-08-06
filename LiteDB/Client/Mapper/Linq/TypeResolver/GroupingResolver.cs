using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB
{
    internal class GroupingResolver : EnumerableResolver
    {
        public override string ResolveMethod(MethodInfo method)
        {
            // all methods in Enumerable are Extensions (static methods), so first parameter is IEnumerable
            var name = Reflection.MethodName(method, 1);

            // only few IEnumerable methods are supported in IGrouping
            // (only when expression are inside)
            switch (name)
            {
                case "Count()":
                case "Sum()":
                case "Average()":
                    return base.ResolveMethod(method);
            }

            return null;
        }

        public override string ResolveMember(MemberInfo member)
        {
            switch (member.Name)
            {
                case "Key": return "@key";
            }

            return base.ResolveMember(member);
        }
    }
}