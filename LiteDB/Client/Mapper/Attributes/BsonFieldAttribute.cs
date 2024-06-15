namespace LiteDB;

using System;

/// <summary>
///     Set a name to this property in BsonDocument
/// </summary>
public class BsonFieldAttribute : Attribute
{
    public string Name { get; set; }

    public BsonFieldAttribute(string name)
    {
        Name = name;
    }

    public BsonFieldAttribute()
    {
    }
}