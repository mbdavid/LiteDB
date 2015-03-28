using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class VersionDB : LiteDatabase
    {
        public VersionDB(string connectionString)
            : base(connectionString)
        {
        }

        protected override void OnVersionUpdate(int newVersion)
        {
            if (newVersion == 1)
            {
                var col = this.GetCollection("customer");

                // add 1
                col.Insert(new BsonDocument());
            }
            else if (newVersion == 2)
            {
                var col = this.GetCollection("customer");

                // add more 3
                col.Insert(new BsonDocument());
                col.Insert(new BsonDocument());
                col.Insert(new BsonDocument());
            }
        }
    }

    [TestClass]
    public class VersionTest
    {
        [TestMethod]
        public void Version_Test()
        {
            var dbf = DB.Path();
            var cs1 = "version=1; filename=" + dbf;
            var cs2 = "version=2; filename=" + dbf;

            using (var db = new VersionDB(cs1))
            {
                var col = db.GetCollection("customer");

                // On initialize db, i insert a first row
                Assert.AreEqual(1, col.Count());
            }

            using (var db = new VersionDB(cs2))
            {
                var col = db.GetCollection("customer");

                // And when update database to version 2, insert another
                Assert.AreEqual(4, col.Count());
            }
        }
    }
}
