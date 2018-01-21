using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Shell
{
    #region Model

    public interface IBase
    {
        int Id { get; set; }
        string Name { get; set; }
    }

    public class ClassA : IBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ClassB : IBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    #endregion

    [TestClass]
    public class Update_Type_Tests
    {
        [TestMethod]
        public void Basic_Shell_Commands()
        {
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var typeA = typeof(ClassA).FullName + ", LiteDB.Tests";
                var typeB = typeof(ClassB).FullName + ", LiteDB.Tests";

                var col = db.GetCollection<IBase>("col");

                col.Insert(new ClassA { Name = "MyClassA" });

                var docA = db.Engine.FindById("col", 1);

                // _type must be class A
                Assert.AreEqual(typeA, docA["_type"].AsString);

                // now, update _type from ClassA to ClassB type
                db.Engine.Run(string.Format("db.col.update _type=\"{0}\" where _type=\"{1}\"", typeB, typeA));

                // get updated document
                var docB = db.Engine.FindById("col", 1);

                // _type must be class B
                Assert.AreEqual(typeB, docB["_type"].AsString);

                // lets deserialize to test
                var classB = col.FindById(1);

                // now test deserialized
                Assert.AreEqual(typeof(ClassB), classB.GetType());

            }
        }
    }
}