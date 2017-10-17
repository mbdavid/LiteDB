using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Database
{
    #region Model

    public class MultiKeyDoc
    {
        public int Id { get; set; }
        public int[] Keys { get; set; }
        public List<Customer> Customers { get; set; }
    }

    #endregion

    [TestClass]
    public class MultiKey_Mapper_Tests
    {
        [TestMethod]
        public void MultiKey_Mapper()
        {
            using (var file = new TempFile())
            using (var db = new LiteDatabase(file.Filename))
            {
                var col = db.GetCollection<MultiKeyDoc>("col");

                col.Insert(new MultiKeyDoc
                {
                    Id = 1,
                    Keys = new int[] { 1, 2, 3 },
                    Customers = new List<Customer>()
                    {
                        new Customer { Name = "John" },
                        new Customer { Name = "Ana" },
                        new Customer { Name = "Doe" }
                    }
                });

                col.Insert(new MultiKeyDoc
                {
                    Id = 2,
                    Keys = new int[] { 2 },
                    Customers = new List<Customer>()
                    {
                        new Customer { Name = "Ana" }
                    }
                });

                col.EnsureIndex(x => x.Keys);
                col.EnsureIndex(x => x.Customers.Select(z => z.Name));

                // Query.EQ("Keys", 2)
                Assert.AreEqual(2, col.Count(x => x.Keys.Contains(2)));

                // Query.StartsWith("Customers.Name", "Ana");
                Assert.AreEqual(2, col.Count(x => x.Customers.Any(z => z.Name.StartsWith("Ana"))));

            }
        }
    }
}