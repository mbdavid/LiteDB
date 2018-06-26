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
using System.Diagnostics;

namespace LiteDB.Tests.Engine
{
    [TestClass]
    public class Sql_Tests
    {
        #region Initialize

        private LiteEngine db;
        private BsonDocument[] person;

        [TestInitialize]
        public void Init()
        {
            db = new LiteEngine();
            person = DataGen.Person(1, 100).ToArray();

            db.Insert("person", person);
        }

        [TestCleanup]
        public void CleanUp()
        {
            db.Dispose();
        }

        [DebuggerHidden]
        private void Invalid(string sql)
        {
            try
            {
                db.Execute(sql);

                Assert.Fail("Must fail with this syntax: " + sql);
            }
            catch (LiteException ex) when (ex.ErrorCode == LiteException.UNEXPECTED_TOKEN) { }
        }

        #endregion

        [TestMethod]
        public void Sql_Insert_Single()
        {
            Assert.AreEqual(1, db.Execute("INSERT INTO col1 VALUES {a:1}").Current.AsInt32);
        }

        [TestMethod]
        public void Sql_Insert_Bulk()
        {
            Assert.AreEqual(3, db.Execute("INSERT INTO col1 VALUES {a:1}, {a:2}, {a:3}").Current.AsInt32);
        }

        [TestMethod]
        public void Sql_Insert_With_Custom_Id()
        {
            db.Execute("INSERT INTO col2 VALUES {a:25}, {a:26}, {a:27} WITH ID=INT");

            Assert.AreEqual(25, db.FindById("col2", 1)["a"].AsInt32);
        }

        [TestMethod]
        public void Sql_Invalid_Commands()
        {
            Invalid("INSERT col2 VALUES {a:1}");
            Invalid("INSERT INTO col2 {a:1}");
            Invalid("INSERT INTO col2 VALUES {a:1} WITH ID-INT");
            Invalid("INSERT INTO col-2 VALUES {a:1}");
            Invalid("INSERT INTO col2 VALUES true");
            Invalid("INSERT INTO col2 VALUES {a:1} WITH ID=INT 9");
        }

        [TestMethod]
        public void Sql_Update_Add_Field_For_All()
        {
            db.Execute("UPDATE person SET { x: 1 }");

            Assert.AreEqual(person.Length, db.Count("person", "x = 1"));
        }

        [TestMethod]
        public void Sql_Update_Change_Field_With_Where()
        {
            Assert.AreEqual(0, db.Count("person", "name = UPPER(name) AND _id = 1"));

            db.Execute("UPDATE person SET { name: UPPER(name) } WHERE _id > 0 AND age > 0");

            Assert.AreEqual(1, db.Count("person", "name = UPPER(name) AND _id = 1"));
        }

        [TestMethod]
        public void Sql_Replace_Single_Document()
        {
            db.Execute("REPLACE person SET { _id, name, age: 10 } WHERE _id = 25");

            var d = db.FindById("person", 25);

            Assert.AreEqual(3, d.Keys.Count);
            Assert.AreEqual(10, d["age"].AsInt32);
        }

        [TestMethod]
        public void Sql_Analyze()
        {
            Assert.AreEqual(0, db.Execute("ANALYZE").Current.AsInt32);
        }

        [TestMethod]
        public void Sql_Vaccum()
        {
            Assert.AreEqual(0, db.Execute("VACCUM").Current.AsInt32);
        }

        [TestMethod]
        public void Sql_Checkpoint()
        {
            Assert.AreEqual(0, db.Execute("CHECKPOINT").Current.AsInt32);
        }

        [TestMethod]
        public void Sql_Transaction()
        {
            var t = db.Execute("BEGIN").Current.AsGuid;
            Assert.AreEqual(t, db.Execute("BEGIN TRANS").Current.AsGuid);

            Assert.IsTrue(db.Execute("COMMIT").Current.AsBoolean);
            Assert.IsFalse(db.Execute("COMMIT TRANS").Current.AsBoolean);
            Assert.IsFalse(db.Execute("COMMIT TRANSACTION").Current.AsBoolean);

            Assert.AreNotEqual(t, db.Execute("BEGIN TRANSACTION").Current.AsGuid);

            Assert.IsTrue(db.Execute("ROLLBACK").Current.AsBoolean);
            Assert.IsFalse(db.Execute("ROLLBACK TRANS").Current.AsBoolean);
            Assert.IsFalse(db.Execute("ROLLBACK TRANSACTION").Current.AsBoolean);
        }

        [TestMethod]
        public void Sql_Create_Drop_Index()
        {
            Assert.IsTrue(db.Execute("CREATE INDEX idx1 ON person ( UPPER($.city) );").Current.AsBoolean);

            Assert.IsTrue(db.Execute("CREATE UNIQUE INDEX idx2 ON person (email)").Current.AsBoolean);
            Assert.IsFalse(db.Execute("CREATE UNIQUE INDEX idx2 ON person ($.email)").Current.AsBoolean);

            Assert.IsTrue(db.Execute("DROP INDEX person.idx1;").Current.AsBoolean);
            Assert.IsTrue(db.Execute("DROP INDEX person.idx2;").Current.AsBoolean);
        }

        [TestMethod]
        public void Sql_Drop_Collection()
        {
            db.Execute("INSERT INTO ncol VALUES {a:1};");

            Assert.IsTrue(db.Execute("DROP COLLECTION ncol;").Current.AsBoolean);
            Assert.IsFalse(db.Execute("DROP COLLECTION ncol;").Current.AsBoolean);
        }

        [TestMethod]
        public void Sql_Multi_Commands()
        {
            var r = db.Execute("INSERT INTO m0 VALUES {a:1}; INSERT INTO m1 VALUES {a:1}; DROP COLLECTION m0;");

            while (r.NextResult()) ;

            Assert.IsTrue(db.GetCollectionNames().Any(x => x == "m0"));
        }

        [TestMethod]
        public void Sql_Select_Simple()
        {
            var r = db.Execute("SELECT $ FROM person");
        }


    }
}