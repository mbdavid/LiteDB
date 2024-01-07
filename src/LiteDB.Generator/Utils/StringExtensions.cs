namespace LiteDB.Generator;

internal static class StringExtensions
{
	public static void FormatText(ref string classSource, CSharpParseOptions? options = null)
	{
		options ??= CSharpParseOptions.Default;

		var sourceCode = CSharpSyntaxTree.ParseText(SourceText.From(classSource, Encoding.UTF8), options);
		var formattedRoot = (CSharpSyntaxNode)sourceCode.GetRoot().NormalizeWhitespace();
		classSource = CSharpSyntaxTree.Create(formattedRoot).ToString();
	}
}