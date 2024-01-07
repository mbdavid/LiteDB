namespace LiteDB.Generator;

internal class Attributes
{
	public const string AttributeMetadataName = "LiteDB.AutoInterfaceAttribute";

	public const string AttributesNamespace = "LiteDB";

	public const string AutoInterfaceClassname = "AutoInterfaceAttribute";

	public static readonly string AttributesSourceCode = $@"

using System;
using System.Diagnostics;

#nullable enable

namespace {AttributesNamespace};

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
internal sealed class {AutoInterfaceClassname} : Attribute
{{
    public {AutoInterfaceClassname}()
    {{
    }}

    public {AutoInterfaceClassname}(Type inheritInterface)
    {{
    }}
}}
";
}