namespace LiteDB.Tests.QueryTest;

using System;
using System.Linq;

public class Person_Tests : IDisposable
{
    protected readonly Person[] local;

    protected ILiteDatabase db;
    protected ILiteCollection<Person> collection;

    public Person_Tests()
    {
        local = DataGen.Person().ToArray();

        db = new LiteDatabase(":memory:");
        collection = db.GetCollection<Person>("person");
        collection.Insert(local);
    }

    public void Dispose()
    {
        db?.Dispose();
    }
}