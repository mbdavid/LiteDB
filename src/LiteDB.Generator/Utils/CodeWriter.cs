namespace LiteDB.Generator;

internal class CodeWriter
{
    private StringBuilder _output = new ();

    private bool _firstChar = true;

    public int TabSize { get; set; } = 4;

    public int Indent { get; set; } = 0;

    private string Tabs => _firstChar? "".PadLeft(this.Indent * this.TabSize, ' ') : "";
    
    public void Write(object? value)
    {
        _output.Append(this.Tabs + (value ?? ""));
        _firstChar = false;
    }

    public void Write(string pattern, object args0) 
        => this.Write(string.Format(pattern, args0));

    public void Write(string pattern, object args0, object args1)
        => this.Write(string.Format(pattern, args0, args1));

    public void Write(string pattern, object args0, object args1, object? args2)
        => this.Write(string.Format(pattern, args0, args1, args2));

    public void Write(string pattern, object args0, object args1, object? args2, object? args3)
        => this.Write(string.Format(pattern, args0, args1, args2, args3));

    public void WriteLine(string value = "")
    {
        _output.AppendLine(this.Tabs + value);
        _firstChar = true;
    }

    public void WriteLine(string pattern, object args0)
        => this.WriteLine(string.Format(pattern, args0));

    public void WriteLine(string pattern, object args0, object args1)
        => this.WriteLine(string.Format(pattern, args0, args1));

    public void WriteFrom(CodeWriter cw)
    {
        foreach(var line in cw.ToString().Split('\n'))
        {
            this.WriteLine(line);
        }
    }

    public void WriteJoin<T>(string separator, IEnumerable<T> values)
        => this.WriteJoin(separator, values, (w, x) => w.Write(x));

    public void WriteJoin<T>(string separator, IEnumerable<T> values, Action<CodeWriter, T> writeAction)
    {
        using var enumerator = values.GetEnumerator();

        if (!enumerator.MoveNext()) return;

        writeAction(this, enumerator.Current);

        if (!enumerator.MoveNext()) return;

        do
        {
            this.Write(separator);
            writeAction(this, enumerator.Current);
        } 
        while (enumerator.MoveNext());
    }

    public void WriteTypeGenericsIfNeeded(INamedTypeSymbol implTypeSymbol)
    {
        if (!implTypeSymbol.IsGenericType) return;

        this.Write("<");
        this.WriteJoin(", ", implTypeSymbol.TypeParameters.Select(x => x.Name));
        this.Write(">");

        this.WriteTypeParameterConstraints(implTypeSymbol.TypeParameters);
    }


    public void WriteMethodParamValues(IParameterSymbol param)
    {
        switch (param.RefKind)
        {
            case RefKind.Ref:
                this.Write("ref ");
                break;
            case RefKind.Out:
                this.Write("out ");
                break;
            case RefKind.In:
                this.Write("in ");
                break;
        }

        if (param.Type.Name == "")
        {
            this.Write("this");
        }
        else
        {
            if (StringExtensions.IsCSharpKeyword(param.Name))
            {
                this.Write("@");
            }

            this.Write(param.Name);
        }
    }

    public void WriteMethodParam(IParameterSymbol param)
    {
        if (param.IsParams)
        {
            this.Write("params ");
        }

        switch (param.RefKind)
        {
            case RefKind.Ref:
                this.Write("ref ");
                break;
            case RefKind.Out:
                this.Write("out ");
                break;
            case RefKind.In:
                this.Write("in ");
                break;
        }

        this.Write(param.Type);
        this.Write(" ");

        if (StringExtensions.IsCSharpKeyword(param.Name))
        {
            this.Write("@");
        }

        this.Write(param.Name);

        if (param.HasExplicitDefaultValue)
        {
            this.WriteParamExplicitDefaultValue(param);
        }
    }

    public void WriteParamExplicitDefaultValue(IParameterSymbol param)
    {
        if (param.ExplicitDefaultValue is null)
        {
            this.Write(" = default");
        }
        else
        {
            switch (param.Type.Name)
            {
                case nameof(String):
                    this.Write(" = \"{0}\"", param.ExplicitDefaultValue);
                    break;
                case nameof(Single):
                    this.Write(" = {0}f", param.ExplicitDefaultValue);
                    break;
                case nameof(Double):
                    this.Write(" = {0}d", param.ExplicitDefaultValue);
                    break;
                case nameof(Decimal):
                    this.Write(" = {0}m", param.ExplicitDefaultValue);
                    break;
                case nameof(Boolean):
                    this.Write(" = {0}", param.ExplicitDefaultValue.ToString().ToLower());
                    break;
                case nameof(Nullable<bool>):
                    this.Write(" = {0}", param.ExplicitDefaultValue.ToString().ToLower());
                    break;
                default:
                    this.Write(" = {0}", param.ExplicitDefaultValue);
                    break;
            }
        }
    }

    public void WriteTypeParameterConstraints(IEnumerable<ITypeParameterSymbol> typeParameters)
    {
        foreach (var typeParameter in typeParameters)
        {
            var constraints = typeParameter.EnumGenericConstraints().ToList();

            if (constraints.Count == 0) break;

            this.Write(" where {0} : ", typeParameter.Name);
            this.WriteJoin(", ", constraints);
        }
    }

    public void WriteSymbolDocsIfPresent(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();

        // omit the fist and last lines to skip the <member> tag

        var reader = new StringReader(xml);
        var lines = new List<string>();

        while (true)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            lines.Add(line);
        }

        for (int i = 1; i < lines.Count - 1; i++)
        {
            var line = lines[i].TrimStart(); // for some reason, 4 spaces are inserted to the beginning of the line
            this.WriteLine("/// {0}", line);
        }
    }

    public override string ToString()
    {
        return _output.ToString();
    }
}
