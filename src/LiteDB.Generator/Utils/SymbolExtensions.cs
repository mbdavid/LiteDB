namespace LiteDB.Generator;

internal static class SymbolExtensions
{
    public static bool TryGetAttribute(
        this ISymbol symbol,
        INamedTypeSymbol attributeType,
        out IEnumerable<AttributeData> attributes)
    {
        attributes = symbol.GetAttributes()
                           .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
        return attributes.Any();
    }

    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
    {
        return symbol.GetAttributes()
                     .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attributeType));
    }

    //Ref: https://stackoverflow.com/questions/27105909/get-fully-qualified-metadata-name-in-roslyn
    public static string GetFullMetadataName(this ISymbol symbol, bool useNameWhenNotFound = false)
    {
        if (IsRootNamespace(symbol))
        {
            return useNameWhenNotFound ? symbol.Name : string.Empty;
        }

        var stringBuilder = new StringBuilder(symbol.MetadataName);
        var last = symbol;

        symbol = symbol.ContainingSymbol;

        while (!IsRootNamespace(symbol))
        {
            if (symbol is ITypeSymbol && last is ITypeSymbol)
            {
                stringBuilder.Insert(0, '+');
            }
            else
            {
                stringBuilder.Insert(0, '.');
            }

            stringBuilder.Insert(0, symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            symbol = symbol.ContainingSymbol;
        }

        var retVal = stringBuilder.ToString();
        if (string.IsNullOrWhiteSpace(retVal) && useNameWhenNotFound)
        {
            return symbol.Name;
        }

        return retVal;
    }

    private static bool IsRootNamespace(ISymbol symbol)
    {
        return symbol is INamespaceSymbol { IsGlobalNamespace: true };
    }

    public static string InferVisibilityModifier(this ISymbol implTypeSymbol)
    {
        return implTypeSymbol.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            _ => "internal",
        };
    }

    public static bool IsPublicOrInternal(this ISymbol symbol)
    {
        return symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
    }

    public static IEnumerable<IMethodSymbol> GetConstrcutors(this INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public &&
                        x.Kind == SymbolKind.Method)
            .Select(x => x as IMethodSymbol)
            .Where(x => x?.MethodKind == MethodKind.Constructor)!;
    }
}