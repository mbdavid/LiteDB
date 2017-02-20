using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    [TestClass]
    public class TransactionTest
    {
        [TestMethod]
        public void TransactionCommit_Test()
        {
            using (var f = new TempFile())
            using (var db = new LiteDatabase(f.Filename))
            {
                var col = db.GetCollection<Person>("Person");

                col.Insert(new Person { Fullname = "John" });
                col.Insert(new Person { Fullname = "Doe" });
                using (var transaction = db.BeginTrans())
                {
                    col.Insert(new Person { Fullname = "Joana" });
                    col.Insert(new Person { Fullname = "Marcus" });
                }

                Assert.AreEqual(4, col.Count());
            }
        }

        [TestMethod]
        public void TransactionRollback_Test()
        {
            using (var f = new TempFile())
            using (var db = new LiteDatabase(f.Filename))
            {
                var col = db.GetCollection<Person>("Person");

                col.Insert(new Person { Fullname = "John" });
                col.Insert(new Person { Fullname = "Doe" });
                using (var transaction = db.BeginTrans())
                {
                    col.Insert(new Person { Fullname = "Joana" });
                    col.Insert(new Person { Fullname = "Marcus" });
                    transaction.Rollback();
                }

                Assert.AreEqual(2, col.Count());
            }
        }

        [TestMethod]
        public void TransactionException_Test()
        {
            using (var f = new TempFile())
            using (var db = new LiteDatabase(f.Filename))
            {
                var col = db.GetCollection<Person>("Person");

                col.Insert(new Person { Fullname = "John" });
                col.Insert(new Person { Fullname = "Doe" });

                try
                {
                    using (var transaction = db.BeginTrans())
                    {
                        col.Insert(new Person { Fullname = "Joana" });
                        col.Insert(new Person { Fullname = "Marcus" });
                        throw new IOException();
                    }
                }
                catch (IOException) { }

                Assert.AreEqual(2, col.Count());
            }
        }

        [TestMethod]
        public void TransactionNestedException_Test()
        {
            using (var f = new TempFile())
            using (var db = new LiteDatabase(f.Filename))
            {
                var col = db.GetCollection<Person>("Person");

                try
                {
                    using (var transaction1 = db.BeginTrans())
                    {
                        col.Insert(new Person { Id = 1, Fullname = "John" });

                        using (var transaction2 = db.BeginTrans())
                        {
                            col.Insert(new Person { Id = 2, Fullname = "Joana" });
                        }

                        col.Insert(new Person { Id = 1, Fullname = "Foo Bar" }); // throws duplicate key exception
                    }

                }
                catch (LiteException) { }

                Assert.AreEqual(0, col.Count());
            }
        }
    }
}
