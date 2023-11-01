using System.Dynamic;

namespace LiteDB.Generator;

internal class InterfaceGen
{
    /// <summary>
    /// Generate all interfaces for classes with [AutoInterface] attribute
    /// </summary>
    public static void GenerateCode(CodeBase codeBase)
    {
        foreach (var cls in codeBase.GetTypesWithAutoInterface())
        {
            var code = GenerateInterfaceCode(cls);

            var filename = cls.TypeSymbol.GetFullMetadataName(useNameWhenNotFound: true);

            codeBase.AddSource($"{filename}_AutoInterface.g.cs", code);
        }
    }

    private static string GenerateInterfaceCode(TypeSymbolWithAttribute type)
    {
        var cw = new CodeWriter();

        var namespaceName = type.TypeSymbol.ContainingNamespace.ToDisplayString();
        var interfaceName = "I" + type.TypeSymbol.Name;
        var visibilityModifier = type.TypeSymbol.InferVisibilityModifier();
        var inheritInterface = type.Attribute.GetConstructorValue();
        var inheritMembers = new HashSet<string>();

        if (inheritInterface is not null)
        {
            var interfaceType = Type.GetType(inheritInterface);

            if (interfaceType is not null)
            {
                foreach (var member in interfaceType.GetMembers())
                {
                    inheritMembers.Add(member.Name);
                }
            }

            inheritInterface = ": " + inheritInterface;
        }

        var @unsafe = "";

        //var sb = new StringBuilder();

        foreach (var item in type.TypeSymbol.DeclaringSyntaxReferences)
        {
            var code = item.SyntaxTree.ToString().Substring(item.Span.Start, item.Span.End - item.Span.Start);

            if (code.Contains("unsafe")) @unsafe = "unsafe ";

            //sb.AppendLine("Código: `" + @unsafe + "`");
        }
        //sb.AppendLine("----------------");
        //File.AppendAllText(@"C:\temp\gener.txt", sb.ToString());

        cw.WriteLine("namespace {0};", namespaceName);
        cw.WriteLine();

        cw.WriteSymbolDocsIfPresent(type.TypeSymbol);
        cw.Write("{0}{1} partial interface {2}{3}", @unsafe, visibilityModifier, interfaceName, inheritInterface);
        cw.WriteTypeGenericsIfNeeded(type.TypeSymbol);
        cw.WriteLine();
        cw.WriteLine("{");
        cw.WriteLine("#nullable enable");

        cw.Indent++;
        GenerateInterfaceMemberDefinitions(cw, type.TypeSymbol, inheritMembers);
        cw.Indent--;

        cw.WriteLine("#nullable disable");

        cw.WriteLine("}");

        return cw.ToString();
    }

    private static void GenerateInterfaceMemberDefinitions(CodeWriter cw, INamespaceOrTypeSymbol implTypeSymbol, HashSet<string> inheritMembers)
    {
        foreach (var member in implTypeSymbol.GetMembers())
        {
            if (member.DeclaredAccessibility != Accessibility.Public) continue;

            if (inheritMembers.Contains(member.Name)) continue;


            GenerateInterfaceMemberDefinition(cw, member);
        }
    }

    private static void GenerateInterfaceMemberDefinition(CodeWriter cw, ISymbol member)
    {
        switch (member)
        {
            case IPropertySymbol propertySymbol:
                GeneratePropertyDefinition(cw, propertySymbol);
                break;
            case IMethodSymbol methodSymbol:
                GenerateMethodDefinition(cw, methodSymbol);
                break;
        }
    }

    private static void GeneratePropertyDefinition(CodeWriter cw, IPropertySymbol propertySymbol)
    {
        if (propertySymbol.IsStatic) return;

        bool hasPublicGetter = propertySymbol.GetMethod is not null &&
                               propertySymbol.GetMethod.IsPublicOrInternal();

        bool hasPublicSetter = propertySymbol.SetMethod is not null &&
                               propertySymbol.SetMethod.IsPublicOrInternal();

        if (!hasPublicGetter && !hasPublicSetter) return;

        cw.WriteSymbolDocsIfPresent(propertySymbol);

        if (propertySymbol.IsIndexer)
        {
            cw.Write("{0} this[", propertySymbol.Type);
            cw.WriteJoin(", ", propertySymbol.Parameters, (cwi, p) => cwi.WriteMethodParam(p));
            cw.Write("] ");
        }
        else
        {
            cw.Write("{0} {1} ", propertySymbol.Type, propertySymbol.Name); // ex. int Foo
        }

        cw.Write("{ ");

        if (hasPublicGetter)
        {
            cw.Write("get; ");
        }

        if (hasPublicSetter)
        {
            if (propertySymbol.SetMethod!.IsInitOnly)
            {
                cw.Write("init; ");
            }
            else
            {
                cw.Write("set; ");
            }
        }

        cw.WriteLine("}");
    }

    private static void GenerateMethodDefinition(CodeWriter cw, IMethodSymbol methodSymbol)
    {
        if (methodSymbol.MethodKind != MethodKind.Ordinary || methodSymbol.IsStatic) return;

        if (methodSymbol.IsImplicitlyDeclared && methodSymbol.Name != "Deconstruct")
        {
            // omit methods that are auto generated by the compiler (eg. record's methods),
            // except for the record Deconstruct method
            return;
        }

        cw.WriteSymbolDocsIfPresent(methodSymbol);

        cw.Write("{0} {1}", methodSymbol.ReturnType, methodSymbol.Name); // ex. int Foo

        if (methodSymbol.IsGenericMethod)
        {
            cw.Write("<");
            cw.WriteJoin(", ", methodSymbol.TypeParameters.Select(x => x.Name));
            cw.Write(">");
        }

        cw.Write("(");
        cw.WriteJoin(", ", methodSymbol.Parameters, (cwi, p) => cwi.WriteMethodParam(p));
        cw.Write(")");

        if (methodSymbol.IsGenericMethod)
        {
            cw.WriteTypeParameterConstraints(methodSymbol.TypeParameters);
        }

        cw.WriteLine(";");
    }
}
