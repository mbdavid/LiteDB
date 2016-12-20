using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class CustomerUniqueIndex
    {
        public int Id { get; set; }

        [BsonIndex(true)]
        public string Name { get; set; }
    }

    [TestClass]
    public class UniqueIndexMapperTest
    {
        [TestMethod]
        public void UniqueIndexMapper_Test()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var c1 = new CustomerUniqueIndex { Name = "John Doe" };
                var c2 = new CustomerUniqueIndex { Name = "Joana" };

                var col = db.GetCollection<CustomerUniqueIndex>("col");

                col.Insert(c1);
                col.Insert(c2);

                // index are created in first indexed query
                var r = col.FindOne(x => x.Name == "Joana");

                var nameIndex = col.GetIndexes().ToArray()[1];

                Assert.IsTrue(nameIndex.Unique);
            }
        }
    }
}