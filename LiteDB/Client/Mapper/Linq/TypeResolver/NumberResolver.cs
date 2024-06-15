namespace LiteDB;

using System.Reflection;

internal class NumberResolver : ITypeResolver
{
    private readonly string _parseMethod;

    public NumberResolver(string parseMethod)
    {
        _parseMethod = parseMethod;
    }

    public string ResolveMethod(MethodInfo method)
    {
        switch (method.Name)
        {
            // instance methods
            case "ToString":
                var pars = method.GetParameters();
                if (pars.Length == 0)
                    return "STRING(#)";
                if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                    return "FORMAT(#, @0)";
                break;

            // static methods
            case "Parse":
                return $"{_parseMethod}(@0)";
            case "Equals":
                return "# = @0";
        }

        ;

        return null;
    }

    public string ResolveMember(MemberInfo member) => null;
    public string ResolveCtor(ConstructorInfo ctor) => null;
}