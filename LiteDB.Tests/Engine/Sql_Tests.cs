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

    }
}