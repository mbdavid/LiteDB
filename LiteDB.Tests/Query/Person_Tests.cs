using System;
using System.Linq;

namespace LiteDB.Tests.QueryTest
{
    public class Person_Tests : IDisposable
    {
        private readonly Person[] _local;
        private readonly ILiteDatabase _db;
        private readonly ILiteCollection<Person> _collection;

        public Person_Tests()
        {
            this._local = DataGen.Person().ToArray();

            _db = new LiteDatabase(":memory:");
            _collection = _db.GetCollection<Person>("person");
            _collection.Insert(this._local);
        }

        public ILiteCollection<Person> GetCollection() => _collection;

        public Person[] GetLocal() => _local;

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}