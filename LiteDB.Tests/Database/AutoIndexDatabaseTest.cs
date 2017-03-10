using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class PersonAutoIndex
    {
        public ObjectId Id { get; set; }

        [BsonField("name"), BsonIndex(true)]
        public string Name { get; set; }

        [BsonField("age"), BsonIndex(true)]
        public int Age { get; set; }
    }

    [TestClass]
    public class AutoIndexDatabaseTest
    {
        [TestMethod]
        public void AutoIndexDatabase_Test()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var people = db.GetCollection<PersonAutoIndex>("people");
                var doc = new PersonAutoIndex
                {
                    Name = "john doe",
                    Age = 40
                };

                people.Insert(doc);

                var result = people.FindOne(x => x.Name == "john doe" && x.Age == 40);

                Assert.AreEqual(doc.Name, result.Name);

                var indexName = people.GetIndexes().FirstOrDefault(x => x.Field == "name");
                var indexAge = people.GetIndexes().FirstOrDefault(x => x.Field == "age");

                // now indexes must be unique defined by Attribute
                Assert.AreEqual(true, indexName.Unique);
                Assert.AreEqual(true, indexAge.Unique);
            }
        }
    }
}