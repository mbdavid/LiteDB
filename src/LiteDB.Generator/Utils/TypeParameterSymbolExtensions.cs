namespace LiteDB.Generator;

internal static class TypeParameterSymbolExtensions
{
    public static IEnumerable<string> EnumGenericConstraints(this ITypeParameterSymbol symbol)
    {
        // the class/struct/unmanaged/notnull constraint has to be the last
        if (symbol.HasNotNullConstraint)
        {
            yield return "notnull";
        }
        
        if (symbol.HasValueTypeConstraint)
        {
            yield return "struct";
        }
        
        if (symbol.HasUnmanagedTypeConstraint)
        {
            yield return "unmanaged";
        }
        
        if (symbol.HasReferenceTypeConstraint)
        {
            yield return symbol.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated
                ? "class?"
                : "class";
        }

        // types go in the middle
        foreach (var constraintType in symbol.ConstraintTypes)
        {
            yield return constraintType.ToDisplayString();
        }
        
        // the new() constraint has to be the last
        if (symbol.HasConstructorConstraint)
        {
            yield return "new()";
        }
    }
}