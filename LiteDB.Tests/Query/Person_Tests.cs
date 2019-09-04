using System;
using System.Linq;

namespace LiteDB.Tests.QueryTest
{
    public class Person_Tests : IDisposable
    {
        protected readonly Person[] local;

        protected ILiteDatabase db;
        protected ILiteCollection<Person> collection;

        public Person_Tests()
        {
            this.local = DataGen.Person().ToArray();

            db = new LiteDatabase(":memory:");
            collection = db.GetCollection<Person>("person");
            collection.Insert(this.local);
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}