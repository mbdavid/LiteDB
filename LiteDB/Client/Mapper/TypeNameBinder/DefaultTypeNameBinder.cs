namespace LiteDB;

using System;
using System.Reflection;

public class DefaultTypeNameBinder : ITypeNameBinder
{
    public static DefaultTypeNameBinder Instance { get; } = new DefaultTypeNameBinder();

    private DefaultTypeNameBinder()
    {
    }

    public string GetName(Type type) => type.FullName + ", " + type.GetTypeInfo().Assembly.GetName().Name;

    public Type GetType(string name) => Type.GetType(name);
}