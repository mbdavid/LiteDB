using CommunityToolkit.Maui.SourceGenerators.Internal.Helpers;

namespace LiteDB.Generator.Models;

readonly record struct ClassInformation(string ClassName, string DeclaredAccessibility, string ContainingNamespace);

readonly record struct InterfaceInformation(ClassInformation ClassInformation, EquatableArray<string> Methods, string MetadataName, bool IsUnsafe);

readonly record struct MemberInfo(string Name, string ReturnType, EquatableArray<string> Parameters);