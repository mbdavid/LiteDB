using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;

namespace LiteDB.Tests.Query
{
    public class Person_Tests
    {
        protected readonly Person[] local;

        protected LiteDatabase db;
        protected LiteCollection<Person> collection;

        public Person_Tests()
        {
            this.local = LoadData().ToArray();
        }

        private IEnumerable<Person> LoadData()
        {
            using (var stream = typeof(DataGen).Assembly.GetManifestResourceStream("LiteDB.Tests.Query.Model.person.json"))
            {
                var reader = new StreamReader(stream);

                var s = reader.ReadToEnd();

                var docs = JsonSerializer.DeserializeArray(s).Select(x => x.AsDocument);
                var id = 0;

                foreach (var doc in docs)
                {
                    yield return new Person
                    {
                        Id = ++id,
                        Name = doc["name"],
                        Age = doc["age"],
                        Phones = doc["phone"].AsString.Split("-"),
                        Email = doc["email"],
                        Date = doc["date"],
                        Active = doc["active"],
                        Address = new Address
                        {
                            Street = doc["street"],
                            City = doc["city"],
                            State = doc["state"]
                        }
                    };
                }
            }
        }

        [TestInitialize]
        public void Init()
        {
            db = new LiteDatabase(":memory:");
            collection = db.GetCollection<Person>("person");
            collection.Insert(this.local);
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }
    }
}