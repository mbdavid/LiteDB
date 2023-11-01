namespace LiteDB.Generator;

internal record TypeSymbolWithAttribute(INamedTypeSymbol TypeSymbol, AttributeData Attribute);

internal class CodeBase
{
    private readonly INamedTypeSymbol _generateAutoInterfaceAttribute = null!;

    private readonly GeneratorExecutionContext _context;
    private readonly Compilation _compilation;

    public CodeBase(GeneratorExecutionContext context)
    {
        _context = context;
        _compilation = GetCompilation(context);

        _generateAutoInterfaceAttribute = _compilation.GetTypeByMetadataName(
            $"{Attributes.AttributesNamespace}.{Attributes.AutoInterfaceClassname}")!;
    }

    public void AddSource(string filename, string code)
    {
        var source = SourceText.From(code, Encoding.UTF8);

        _context.AddSource(filename, source);
    }

    private IEnumerable<INamedTypeSymbol> GetImplTypeSymbols()
    {
        if (_context.SyntaxReceiver is not SyntaxReceiver receiver) yield break;

        foreach(var symbol in receiver.CandidateTypes.Select(candidate => GetTypeSymbol(candidate)))
        {
            yield return symbol;
        }
    }

    private INamedTypeSymbol GetTypeSymbol(SyntaxNode type)
    {
        var model = _compilation.GetSemanticModel(type.SyntaxTree);
        var typeSymbol = model.GetDeclaredSymbol(type)!;
        return (INamedTypeSymbol)typeSymbol;
    }

    /// <summary>
    /// Returns all classes/structs symbols in code-base that contains [AutoInterface]
    /// </summary>
    public IEnumerable<TypeSymbolWithAttribute> GetTypesWithAutoInterface()
    {
        var classSymbols = this.GetImplTypeSymbols();

        var classSymbolNames = new List<string>();

        foreach (var implTypeSymbol in classSymbols)
        {
            if (!implTypeSymbol.TryGetAttribute(_generateAutoInterfaceAttribute, out var attributes))
            {
                continue;
            }

            if (classSymbolNames.Contains(implTypeSymbol.GetFullMetadataName(useNameWhenNotFound: true)))
            {
                continue; // partial class, already added
            }

            classSymbolNames.Add(implTypeSymbol.GetFullMetadataName(useNameWhenNotFound: true));

            var attribute = attributes.Single();

            yield return new TypeSymbolWithAttribute(implTypeSymbol, attribute);
        }
    }

    private static Compilation GetCompilation(GeneratorExecutionContext context)
    {
        var options = context.Compilation.SyntaxTrees.First().Options as CSharpParseOptions;

        var compilation = context.Compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(
                SourceText.From(Attributes.AttributesSourceCode, Encoding.UTF8), options));

        return compilation;
    }

}
