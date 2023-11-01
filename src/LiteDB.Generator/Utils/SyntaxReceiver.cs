namespace LiteDB.Generator;

internal class SyntaxReceiver : ISyntaxReceiver
{
    public IList<TypeDeclarationSyntax> CandidateTypes { get; } = new List<TypeDeclarationSyntax>();
    
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is TypeDeclarationSyntax typeDeclarationSyntax &&
            IsClassOrRecord(typeDeclarationSyntax) &&
            typeDeclarationSyntax.AttributeLists.Count > 0)
        {
            this.CandidateTypes.Add(typeDeclarationSyntax);
        }
    }

    private static bool IsClassOrRecord(TypeDeclarationSyntax typeDeclarationSyntax)
    {
        return typeDeclarationSyntax is ClassDeclarationSyntax || typeDeclarationSyntax is RecordDeclarationSyntax;
    }
}