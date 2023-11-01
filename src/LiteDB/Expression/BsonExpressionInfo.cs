namespace LiteDB;

internal readonly struct BsonExpressionInfo
{
    /// <summary>
    /// Indicate that expression contains a root $ but without any path navigation (should load full document)
    /// </summary>
    public readonly bool FullRoot;

    /// <summary>
    /// Get root fields keys used in document (empty array if no fields found)
    /// </summary>
    public readonly string[] RootFields;

    /// <summary>
    /// Indicate that this expression can result a diferent result for a same input arguments
    /// </summary>
    public readonly bool IsVolatile;

    /// <summary>
    /// Return if this expression contains @ parameters
    /// </summary>
    public readonly bool HasParameters;

    /// <summary>
    /// Check if this expression can be used in index expression (contains no paramter or volatile method calls)
    /// </summary>
    public bool IsIndexable => this.HasDocumentAccess && !this.IsVolatile;

    /// <summary>
    /// Return  is this expression (or any children) access the root document
    /// </summary>
    public bool HasDocumentAccess => this.FullRoot || this.RootFields.Length > 0;

    /// <summary>
    /// Get some expression infos reading full expression tree
    /// </summary>
    public BsonExpressionInfo(BsonExpression expr)
    {
        if (expr.IsEmpty)
        {
            this.RootFields = Array.Empty<string>();

            return;
        }

        var rootFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        this.GetInfo(expr, rootFields, ref this.IsVolatile, ref this.FullRoot, ref this.HasParameters);

        this.RootFields = rootFields.Count == 0 ? 
            Array.Empty<string>() :  // avoid new empty array
            rootFields.ToArray();
    }

    private void GetInfo(BsonExpression expr, HashSet<string> rootFields, ref bool isVolatile, ref bool fullRoot, ref bool hasParameters)
    {
        // get root fields from path
        if (expr is PathBsonExpression path)
        {
            if (path.Source.Type == BsonExpressionType.Root)
            {
                if (path.Field.Length > 0)
                {
                    rootFields.Add(path.Field);
                }
                else
                {
                    fullRoot = true;
                }

                // avoid enter on path children
                return;
            }
        }
        // $ root sign with no path navigation
        else if (expr.Type == BsonExpressionType.Root)
        {
            fullRoot = true;
        }
        // call methods mark as [Volatile]
        else if (expr.Type == BsonExpressionType.Call)
        {
            var call = (CallBsonExpression)expr;

            if (call.IsVolatile == true)
            {
                isVolatile = true;
            }
        }
        // parameters are volatile
        else if (expr.Type == BsonExpressionType.Parameter)
        {
            hasParameters = true;
            isVolatile = true;
        }

        // apply for all children recursive
        foreach(var child in expr.Children)
        {
            this.GetInfo(child, rootFields, ref isVolatile, ref fullRoot, ref hasParameters);
        }
    }

    public override string ToString()
    {
        return Dump.Object(new { FullRoot, RootFields, IsVolatile, HasParameters, IsIndexable, HasDocumentAccess });
    }
}
