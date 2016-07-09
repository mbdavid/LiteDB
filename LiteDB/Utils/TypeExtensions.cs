#if NETFULL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class TypeExtensions
{
    // In 4.5+, TypeInfo has most of the reflection methods previously on type
    // This allows code to be shared between 3.5 && 4.5+ projects
    public static Type GetTypeInfo(this Type type)
    {
        return type;
    }
}
#endif