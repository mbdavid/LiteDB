using LiteDB.Generator.Models;
using System.Collections.Immutable;
using System.Threading;

namespace LiteDB.Generator;

[Generator]
public sealed class LiteGenerator : IIncrementalGenerator
{
	static CSharpParseOptions? parseOptions;
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// To Debug uncomment the following line and set a breakpoint
		//Debugger.Launch();
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource(Attributes.AutoInterfaceClassname, SourceText.From(Attributes.AttributesSourceCode, Encoding.UTF8)));

		var provider = context.SyntaxProvider.ForAttributeWithMetadataName(Attributes.AttributeMetadataName,
		SyntaxPredicate, SemanticTransform)
		.Where(static x => x != emptyInterfaceInformation)
		.Collect();

		context.RegisterSourceOutput(provider, Execute);
	}

	static void Execute(SourceProductionContext context, ImmutableArray<InterfaceInformation> interfaces)
	{
		var sb = new StringBuilder();
		foreach (var (classInfo, publicMembers, inheritInterface, isUnsafe) in interfaces)
		{
			var unsafeString = isUnsafe ? "unsafe " : string.Empty;
			sb.Append(
$$"""
//Last generated: {{DateTime.Now}}
#nullable enable
namespace {{classInfo.ContainingNamespace}};

{{classInfo.DeclaredAccessibility}} {{unsafeString}} partial interface I{{classInfo.ClassName}}
""");

			if (string.IsNullOrEmpty(inheritInterface))
			{
				sb.AppendLine().AppendLine("{");
			}
			else
			{
				sb.AppendLine($" : {inheritInterface}").AppendLine("{");
			}

			foreach (var member in publicMembers)
			{
				sb.AppendLine(member);
			}

			sb.Append('}');

			var source = sb.ToString();
			StringExtensions.FormatText(ref source, parseOptions);

			context.AddSource($"{classInfo.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
			sb.Clear();
		}
	}

	static InterfaceInformation emptyInterfaceInformation = new(default, ImmutableArray<string>.Empty, string.Empty, false);

	static InterfaceInformation SemanticTransform(GeneratorAttributeSyntaxContext context, CancellationToken token)
	{
		var targetNode = context.TargetNode;
		var options = targetNode.SyntaxTree.Options;

		if (options is CSharpParseOptions csharpParseOptions && parseOptions is null)
		{
			parseOptions = csharpParseOptions;
		}

		if (targetNode is not ClassDeclarationSyntax or StructDeclarationSyntax)
		{
			return emptyInterfaceInformation;
		}

		var semanticModel = context.SemanticModel;

		var classSymbol = (INamedTypeSymbol?)semanticModel.GetDeclaredSymbol(context.TargetNode, token);
		if (classSymbol is null)
		{
			return emptyInterfaceInformation;
		}

		var baseInterface = GetInheritedInterface(context);

		var members = baseInterface?.GetMembers() ?? ImmutableArray<ISymbol>.Empty;

		var classInfo = new ClassInformation(classSymbol.Name, classSymbol.DeclaredAccessibility.ToString().ToLower(), classSymbol.ContainingNamespace.ToDisplayString());

		var publicMethods = classSymbol.GetMembers().OfType<IMethodSymbol>()
		.Where(m => m.DeclaredAccessibility == Accessibility.Public && m.MethodKind != MethodKind.Constructor && m.MethodKind != MethodKind.PropertyGet && m.MethodKind != MethodKind.PropertySet)
		.Select(y =>
		{
			if (members.Any(x => x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase)))
			{
				return null;
			}
			return y;
		});

		var publicProperties = classSymbol.GetMembers().OfType<IPropertySymbol>().Where(m => m.DeclaredAccessibility == Accessibility.Public)
		.Select(y =>
		{
			if (members.Any(x => x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase)))
			{
				return null;
			}
			return y;
		});

		var listOfMembers = new List<string>();
		var sb = new StringBuilder();
		var isUnsafe = false;

		token.ThrowIfCancellationRequested();
		CodeWriterHelpers.ProcessProperties(publicProperties, listOfMembers, sb, ref isUnsafe);
		token.ThrowIfCancellationRequested();
		CodeWriterHelpers.ProcessMethods(publicMethods, listOfMembers, sb, ref isUnsafe);

		return new InterfaceInformation(classInfo, listOfMembers.ToImmutableArray(), baseInterface?.ToDisplayString() ?? string.Empty, isUnsafe);

		static INamedTypeSymbol? GetInheritedInterface(GeneratorAttributeSyntaxContext context)
		{
			var attributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName(Attributes.AttributeMetadataName);
			var attributeData = context.Attributes.FirstOrDefault(x => x.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) ?? false);
			var ctorData = attributeData?.GetConstructorValue() ?? string.Empty;

			return context.SemanticModel.Compilation.GetTypeByMetadataName(ctorData);
		}
	}

	static bool SyntaxPredicate(SyntaxNode node, CancellationToken _) =>
		node is TypeDeclarationSyntax { AttributeLists.Count: > 0 };
}