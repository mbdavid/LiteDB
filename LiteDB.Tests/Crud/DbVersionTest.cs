using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace LiteDB.Tests
{
    public class VerDatabase : LiteDatabase
    {
        public static int STEP = 1; // For debug tests

        public VerDatabase(Stream s)
            : base(s)
        {
        }

        protected override void OnDbVersionUpdate(DbVersion dv)
        {
            dv.Register(1, (db) => db.Run("db.col1.insert {a:1}"));

            if(STEP == 2)
            {
                dv.Register(2, (db) => db.Run("db.col2.insert {a:1}"));
            }
            if (STEP == 3)
            {
                dv.Register(3, (db) => db.Run("db.col3.insert {a:1}"));
            }
        }
    }


    [TestClass]
    public class DbVersionTest
    {
        [TestMethod]
        public void DbVerion_Test()
        {
            var m = new MemoryStream();

            using (var db = new VerDatabase(m))
            {
                Assert.AreEqual(true, db.CollectionExists("col1"));
                Assert.AreEqual(false, db.CollectionExists("col2"));
                Assert.AreEqual(false, db.CollectionExists("col3"));
            }

            VerDatabase.STEP = 2; // to simulate changes in time

            using (var db = new VerDatabase(m))
            {
                Assert.AreEqual(true, db.CollectionExists("col1"));
                Assert.AreEqual(true, db.CollectionExists("col2"));
                Assert.AreEqual(false, db.CollectionExists("col3"));
            }

            VerDatabase.STEP = 3; // to simulate changes in time

            using (var db = new VerDatabase(m))
            {
                Assert.AreEqual(true, db.CollectionExists("col1"));
                Assert.AreEqual(true, db.CollectionExists("col2"));
                Assert.AreEqual(true, db.CollectionExists("col3"));
            }

        }
    }
}
