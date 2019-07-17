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
using System.Threading;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Transactions_Tests
    {
        [TestMethod]
        public void Transaction_Write_Lock_Timeout()
        {
            var data1 = DataGen.Person(1, 100).ToArray();
            var data2 = DataGen.Person(101, 200).ToArray();

            using (var db = new LiteDatabase("filename=:memory:;timeout=1"))
            {
                var person = db.GetCollection<Person>();

                // init person collection with 100 document
                person.Insert(data1);

                // task A will open transaction and will insert +100 documents 
                // but will commit only 2s later
                var ta = new Task(() =>
                {
                    db.BeginTrans();

                    person.Insert(data2);

                    Task.Delay(2000).Wait();

                    var count = person.Count();

                    Assert.AreEqual(data1.Length + data2.Length, count);

                    db.Commit();
                });

                // task B will try delete all documents but will be locked during 1 second
                var tb = new Task(() =>
                {
                    Task.Delay(250).Wait();

                    db.BeginTrans();

                    try
                    {
                        person.DeleteMany("1 = 1");

                        Assert.Fail("Must be locked");
                    }
                    catch (LiteException ex) when (ex.ErrorCode == LiteException.LOCK_TIMEOUT) { }

                });

                ta.Start();
                tb.Start();

                Task.WaitAll(ta, tb);
            }
        }


        [TestMethod]
        public void Transaction_Avoid_Drity_Read()
        {
            var data1 = DataGen.Person(1, 100).ToArray();
            var data2 = DataGen.Person(101, 200).ToArray();

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var person = db.GetCollection<Person>();

                // init person collection with 100 document
                person.Insert(data1);

                // task A will open transaction and will insert +100 documents 
                // but will commit only 1s later - this plus +100 document must be visible only inside task A
                var ta = new Task(() =>
                {
                    db.BeginTrans();

                    person.Insert(data2);

                    Task.Delay(1000).Wait();

                    var count = person.Count();

                    Assert.AreEqual(data1.Length + data2.Length, count);

                    db.Commit();
                });

                // task B will not open transaction and will wait 250ms before and count collection - 
                // at this time, task A already insert +100 document but here I cann't see (are not commited yet)
                // after task A finish, I can see now all 200 documents
                var tb = new Task(() =>
                {
                    Task.Delay(250).Wait();

                    var count = person.Count();

                    // read 100 documents
                    Assert.AreEqual(data1.Length, count);

                    ta.Wait();

                    // read 200 documets
                    count = person.Count();

                    Assert.AreEqual(data1.Length + data2.Length, count);
                });

                ta.Start();
                tb.Start();

                Task.WaitAll(ta, tb);
            }
        }

        [TestMethod]
        public void Transaction_Read_Version()
        {
            var data1 = DataGen.Person(1, 100).ToArray();
            var data2 = DataGen.Person(101, 200).ToArray();

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var person = db.GetCollection<Person>();

                // init person collection with 100 document
                person.Insert(data1);

                // task A will insert more 100 documents but will commit only 1s later
                var ta = new Task(() =>
                {
                    db.BeginTrans();

                    person.Insert(data2);

                    Task.Delay(1000).Wait();

                    db.Commit();
                });

                // task B will open transaction too and will count 100 original documents only
                // but now, will wait task A finish - but is in transaction and must see only initial version
                var tb = new Task(() =>
                {
                    db.BeginTrans();

                    Task.Delay(250).Wait();

                    var count = person.Count();

                    // read 100 documents
                    Assert.AreEqual(data1.Length, count);

                    ta.Wait();

                    // keep reading 100 documets because i'm still in same transaction
                    count = person.Count();

                    Assert.AreEqual(data1.Length, count);
                });

                ta.Start();
                tb.Start();

                Task.WaitAll(ta, tb);
            }
        }

        [TestMethod]
        public void Test_Transaction_States()
        {
            var data0 = DataGen.Person(1, 10).ToArray();
            var data1 = DataGen.Person(11, 20).ToArray();

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var person = db.GetCollection<Person>();

                // first time transaction will be opened
                Assert.IsTrue(db.BeginTrans());

                // but in second type transaction will be same
                Assert.IsFalse(db.BeginTrans());

                person.Insert(data0);

                // must commit transaction
                Assert.IsTrue(db.Commit());

                // no transaction to commit
                Assert.IsFalse(db.Commit());

                // no transaction to rollback;
                Assert.IsFalse(db.Rollback());

                Assert.IsTrue(db.BeginTrans());

                // no page was changed but ok, let's rollback anyway
                Assert.IsTrue(db.Rollback());

                // auto-commit
                person.Insert(data1);

                Assert.AreEqual(20, person.Count());
            }
        }
    }
}