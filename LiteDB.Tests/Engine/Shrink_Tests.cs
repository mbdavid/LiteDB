using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LiteDB.Engine;

//** namespace LiteDB.Tests.Engine
//** {
//**     [TestClass]
//**     public class Shrink_Tests
//**     {
//**         [TestMethod]
//**         public void Shrink_After_DropCollection()
//**         {
//**             using (var file = new TempFile())
//**             using (var db = new LiteDatabase(file.Filename))
//**             {
//**                 var col = db.GetCollection<Zip>();
//** 
//**                 col.Insert(DataGen.Zip());
//** 
//**                 db.DropCollection("col");
//** 
//**                 // full disk usage
//**                 var size = file.Size;
//** 
//**                 var r = db.Shrink();
//** 
//**                 // only header page
//**                 Assert.AreEqual(8192, size - r);
//**             }
//**         }
//** 
//**         [TestMethod]
//**         public void Shrink_Large_Files()
//**         {
//**             // do some tests
//**             void DoTest(LiteDatabase db, LiteCollection<Zip> col)
//**             {
//**                 Assert.AreEqual(1, col.Count());
//**                 Assert.AreEqual(99, db.UserVersion);
//**             };
//** 
//**             using (var file = new TempFile())
//**             {
//**                 using (var db = new LiteDatabase(file.Filename))
//**                 {
//**                     var col = db.GetCollection<Zip>();
//** 
//**                     db.UserVersion = 99;
//** 
//**                     col.EnsureIndex("city", false);
//** 
//**                     var inserted = col.Insert(DataGen.Zip()); // 29.353 docs
//**                     var deleted = col.DeleteMany(x => x.Id != "01001"); // delete 29.352 docs
//** 
//**                     Assert.AreEqual(29353, inserted);
//**                     Assert.AreEqual(29352, deleted);
//** 
//**                     Assert.AreEqual(1, col.Count());
//** 
//**                     // must checkpoint
//**                     db.Checkpoint();
//** 
//**                     // file still large than 5mb (even with only 1 document)
//**                     Assert.IsTrue(file.Size > 5 * 1024 * 1024);
//** 
//**                     // reduce datafile (use temp disk) (from LiteDatabase)
//**                     var reduced = db.Shrink();
//** 
//**                     // now file are small than 50kb
//**                     Assert.IsTrue(file.Size < 50 * 1024);
//** 
//**                     DoTest(db, col);
//**                 }
//** 
//**                 // re-open and shrink again
//**                 using (var db = new LiteDatabase(file.Filename))
//**                 {
//**                     var col = db.GetCollection<Zip>();
//** 
//**                     DoTest(db, col);
//** 
//**                     db.Shrink();
//** 
//**                     DoTest(db, col);
//**                 }
//**             }
//**         }
//**     }
//** }