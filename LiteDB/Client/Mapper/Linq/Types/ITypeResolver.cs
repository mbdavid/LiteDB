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
    internal interface ITypeResolver
    {
        string ResolveMethod(MethodInfo method);

        bool HasSpecialMember { get; }
        string ResolveMember(MemberInfo member);
    }
}