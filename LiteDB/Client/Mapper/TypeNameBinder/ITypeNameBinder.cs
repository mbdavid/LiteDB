namespace LiteDB;

using System;

public interface ITypeNameBinder
{
    string GetName(Type type);
    Type GetType(string name);
}