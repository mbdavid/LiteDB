namespace LiteDB.Stress;

using System;

public interface ITestItem
{
    string Name { get; }
    int TaskCount { get; }
    TimeSpan Sleep { get; }
    BsonValue Execute(LiteDatabase db);
}