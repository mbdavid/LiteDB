//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace LiteDB.Tests
//{
//    public class MissingIdDoc
//    {
//        public string Name { get; set; }
//        public int Age { get; set; }
//    }

//    [TestClass]
//    public class MissingIdDocTest
//    {
//        [TestMethod]
//        public void MissingIdDoc_Test()
//        {
//            using (var file = new TempFile())
//            using (var db = new LiteDatabase(file.Filename))
//            {
//                var col = db.GetCollection<MissingIdDoc>("col");

//                var p = new MissingIdDoc { Name = "John", Age = 39 };

//                // ObjectID will be generated 
//                var id = col.Insert(p);

//                p.Age = 41;

//                col.Update(id, p);

//                var r = col.FindById(id);

//                Assert.AreEqual(p.Name, r.Name);
//            }
//        }
//    }
//}