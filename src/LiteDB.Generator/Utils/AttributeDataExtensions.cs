namespace LiteDB.Generator;

internal static class AttributeDataExtensions
{
    public static string? GetNamedParamValue(this AttributeData attributeData, string paramName)
    {
        var pair = attributeData.NamedArguments.FirstOrDefault(x => x.Key == paramName);
        return pair.Value.Value?.ToString();
    }

    public static string? GetConstructorValue(this AttributeData attributeData)
    {
        var value = attributeData.ConstructorArguments.FirstOrDefault();
        return value.Value?.ToString();
    }
}