using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Mapper
{
    #region Model

    public class MyBase
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }

    public class Descendant1 : MyBase
    {
        public string Field1 { get; set; }
    }

    public class Descendant2 : MyBase
    {
        public string Field2 { get; set; }
    }

    public class Container
    {
        public Guid Id { get; set; }
        public List<MyBase> Bases { get; set; }
    }

    #endregion

    [TestClass]
    public class Polymorphic_Tests
    {
        [TestMethod]
        public void Simple_Polymorphics()
        {
            using (var file = new TempFile())
            {
                using (var db = new LiteDatabase(file.Filename))
                {
                    var col = db.GetCollection<MyBase>("col1");

                    col.Insert(new Descendant1() { Id = 1 });
                    col.Insert(new Descendant2() { Id = 2 });
                }

                using (var db = new LiteDatabase(file.Filename))
                {
                    var col = db.GetCollection<MyBase>("col1");

                    var d1 = col.FindById(1);
                    var d2 = col.FindById(2);

                    Assert.AreEqual(typeof(Descendant1), d1.GetType());
                    Assert.AreEqual(typeof(Descendant2), d2.GetType());
                }
            }
        }
    }
}