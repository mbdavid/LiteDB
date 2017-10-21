//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections;
//using System.Collections.Generic;
//using System.Text;

//namespace LiteDB.Tests.Storage
//{
//    [TestClass]
//    public class Store_Local_File_Tests
//    {
//        [TestMethod]
//        public void Store_Local_File()
//        {
//            var pdb = "LiteDB.pdb";
//            var len = new FileInfo(pdb).Length; // get original file length

//            using (var file = new TempFile())
//            {
//                // first open to insert file
//                using (var db = new LiteDatabase(file.Filename))
//                {
//                    var f0 = db.FileStorage.Upload("f0", pdb);

//                    Assert.AreEqual(len, f0.Length);
//                }

//                // open datafile to check if was uploaded
//                using (var db = new LiteDatabase(file.Filename))
//                {
//                    using (var mem = new MemoryStream())
//                    {
//                        var f0 = db.FileStorage.Download("f0", mem);

//                        // test length by FileInfo
//                        Assert.AreEqual(len, f0.Length);

//                        // and test by array
//                        Assert.AreEqual(len, mem.ToArray().Length);
//                    }
//                }
//            }
//        }
//    }
//}