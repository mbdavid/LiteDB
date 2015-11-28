using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Security;

namespace LiteDB.Tests
{
    public class VirtualFieldEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class VirtualFieldDatabase : LiteDatabase
    {
        public VirtualFieldDatabase()
            : base(new MemoryStream())
        {
        }

        protected override void OnModelCreating(BsonMapper mapper)
        {
            mapper.Entity<VirtualFieldEntity>()
                .Index("name_length", (c) => c.Name.Length);
        }
    }

    [TestClass]
    public class VirtualFieldTest
    {
        [TestMethod]
        public void VirtualField_Test()
        {
            using (var db = new VirtualFieldDatabase())
            {
                var col = db.GetCollection<VirtualFieldEntity>("col1");

                col.Insert(new VirtualFieldEntity { Name = "John" });
                col.Insert(new VirtualFieldEntity { Name = "Doe" });
                col.Insert(new VirtualFieldEntity { Name = "#1" });

                // auto create index "name_length"
                var q = col.FindOne(Query.EQ("name_length", 4));

                Assert.IsNotNull(q);
                Assert.AreEqual("John", q.Name);
            }
        }
    }
}
