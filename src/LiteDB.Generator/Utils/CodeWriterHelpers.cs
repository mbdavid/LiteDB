using System.Runtime.CompilerServices;

namespace LiteDB.Generator;
static class CodeWriterHelpers
{
	public static void ProcessMethods(IEnumerable<IMethodSymbol?> symbols, List<string> listOfMembers, StringBuilder sb, ref bool isUnsafe)
	{
		foreach (var pm in symbols)
		{
			if (pm is null)
			{
				continue;
			}
			var hasParameters = pm.Parameters.Length > 0;

			if (!isUnsafe)
			{
				isUnsafe = pm.ReturnType.TypeKind == TypeKind.Pointer;
			}

			sb.Append(pm.ReturnType.ToDisplayString())
				.Append(' ')
				.Append(pm.Name);

			if (!hasParameters)
			{
				sb.Append("();");
			}
			else
			{
				sb.Append('(');

				foreach (var p in pm.Parameters)
				{
					if (!isUnsafe)
					{
						isUnsafe = p.Type.TypeKind == TypeKind.Pointer;
					}

					sb.Append(p.ToDisplayString());

					if (p.HasExplicitDefaultValue)
					{
						sb.Append(" = ");
						if (p.Type.SpecialType == SpecialType.System_String)
						{
							sb.Append($" \"{p.ExplicitDefaultValue}\" ");
						}
						else if (p.ExplicitDefaultValue == default)
						{
							sb.Append("default");
						}
						else
						{
							sb.Append(p.ExplicitDefaultValue);
						}
					}

					sb.Append(", ");
				}

				RemoveLastComma(sb);
				sb.Append(");");
			}

			listOfMembers.Add(sb.ToString());
			sb.Clear();
		}
	}

	public static void ProcessProperties(IEnumerable<IPropertySymbol?> symbols, List<string> listOfMembers, StringBuilder sb, ref bool isUnsafe)
	{
		foreach (var symbol in symbols)
		{
			if (symbol is null)
			{
				continue;
			}

			if (!isUnsafe)
			{
				isUnsafe = symbol.Type.TypeKind == TypeKind.Pointer;
			}

			DefineProperty(sb, symbol);
			listOfMembers.Add(sb.ToString());
			sb.Clear();
		}

		static bool DefineProperty(StringBuilder sb, IPropertySymbol symbol) => symbol switch
		{
			{ IsIndexer: true } => WriteIndexerProperty(sb, symbol),
			_ => WriteSimpleProperty(sb, symbol)
		};

		static bool WriteSimpleProperty(StringBuilder sb, IPropertySymbol symbol)
		{
			sb.Append(symbol.Type.ToDisplayString())
				.Append(' ')
				.Append(symbol.Name);

			WritePropertyMethods(sb, symbol);
			return true;
		}

		static bool WriteIndexerProperty(StringBuilder sb, IPropertySymbol symbol)
		{
			sb.Append(symbol.Type.ToDisplayString())
				.Append(' ')
				.Append("this[");

			WriteParameters(sb, symbol);

			sb.Append("] ");

			WritePropertyMethods(sb, symbol);

			return true;
		}

		static void WritePropertyMethods(StringBuilder sb, IPropertySymbol symbol)
		{

			sb.Append('{');

			if (symbol.GetMethod is not null)
			{
				sb.Append(" get; ");
			}

			if (symbol.SetMethod is IMethodSymbol methodSymbol && methodSymbol.DeclaredAccessibility == Accessibility.Public)
			{
				if (!methodSymbol.IsInitOnly)
				{
					sb.Append(" set; ");
				}
				else
				{
					sb.Append(" init; ");
				}
			}
			sb.Append('}');
		}
	}

	static void WriteParameters(StringBuilder sb, IPropertySymbol symbol)
	{
		foreach (var parameter in symbol.Parameters)
		{
			sb.Append(parameter.Type)
				.Append(' ')
				.Append(parameter.Name)
				.Append(", ");
		}

		RemoveLastComma(sb);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static void RemoveLastComma(StringBuilder sb)
	{
		sb.Remove(sb.Length - 2, 2);
	}
}
