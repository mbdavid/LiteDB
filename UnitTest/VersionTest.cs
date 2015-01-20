using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class VersionDB : LiteEngine
    {
        public VersionDB(string connectionString)
            : base(connectionString)
        {
        }

        protected override void OnVersionUpdate(int newVersion)
        {
            if (newVersion == 1)
            {
                Debug.Print("Initializing version");

                this.Customers.Insert(new Customer { CustomerId = Guid.NewGuid(), Name = "First" });

            }
            else if (newVersion == 2)
            {
                Debug.Print("Updating to vesion 2");

                this.Customers.Insert(new Customer { CustomerId = Guid.NewGuid(), Name = "Second" });
                this.Customers.EnsureIndex("Name");
            }
        }

        public Collection<Customer> Customers { get { return this.GetCollection<Customer>("curtomers"); } }
    }

    [TestClass]
    public class VersionTest
    {
        [TestMethod]
        public void Version_Test()
        {
            var cs1 = DB.Path(true, "test.db");
            var cs2 = DB.Path(false, "test.db", "version=2");

            using (var db = new VersionDB(cs1))
            {
                // On initialize db, i insert a first row
                Assert.AreEqual(1, db.Customers.Count());
            }

            using (var db = new VersionDB(cs2))
            {
                // And when update database to version 2, insert another
                Assert.AreEqual(2, db.Customers.Count());
            }
        }
    }
}
