using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    internal interface IResolveType
    {
        bool HasSpecialMember { get; }

        string ResolveMethod(MethodInfo method);

        string ResolveMember(MemberInfo member);
    }
}