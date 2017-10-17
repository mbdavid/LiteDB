using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace LiteDB
{
    internal static class TypeInfoExtensions
    {
#if HAVE_TYPE_INFO
        public static Type[] GetGenericArguments(this TypeInfo type)
        {
            return type.GenericTypeArguments;
        }
#else
        // In 4.5+, TypeInfo has most of the reflection methods previously on type
        // This allows code to be shared between 3.5 && 4.5+ projects
        public static Type GetTypeInfo(this Type type)
        {
            return type;
        }
#endif
    }
}
