namespace LiteDB;

using System.Reflection;

internal class NullableResolver : ITypeResolver
{
    public string ResolveMethod(MethodInfo method)
    {
        return null;
    }

    public string ResolveMember(MemberInfo member)
    {
        switch (member.Name)
        {
            case "HasValue":
                return "(IS_NULL(#) = false)";
            case "Value":
                return "#";
        }

        return null;
    }

    public string ResolveCtor(ConstructorInfo ctor) => null;
}