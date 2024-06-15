namespace LiteDB;

using System;

/// <summary>
///     When a method are decorated with this attribute means that this method are not immutable
/// </summary>
internal class VolatileAttribute : Attribute
{
}