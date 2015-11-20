using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;

namespace UnitTest
{
    [TestClass]
    public class FilesTest
    {
        private const string dbpath = @"C:\Temp\files.ldb";

        [TestInitialize]
        public void Init()
        {
            File.Delete(dbpath);
        }

        [TestMethod]
        public void Files_Store()
        {
            using (var db = new LiteEngine(dbpath))
            {
                var c = db.GetCollection("customer");

                db.BeginTrans();

                for (var i = 1; i <= 500; i++)
                {
                    var d = new BsonDocument();
                    d["Name"] = "San Jose";

                    c.Insert(i, d);
                }
                for (var i = 1; i <= 500; i++)
                {
                    c.Delete(i);
                }

                db.Commit();


                Dump.Pages(db, "before");

                var meta = new Dictionary<string, string>();
                meta["my-data"] = "Google LiteDB";

                db.Storage.Upload("my/foto1.jpg", new MemoryStream(new byte[5000]), meta);

                Dump.Pages(db ,"after file");

                var f = db.Storage.FindByKey("my/foto1.jpg");

                Assert.AreEqual(5000, f.Length);
                Assert.AreEqual("Google LiteDB", f.Metadata["my-data"]);

                var mem = new MemoryStream();

                f.OpenRead(db).CopyTo(mem);

                // file real size after read all bytes
                Assert.AreEqual(5000, mem.Length);

                // all bytes are 0
                Assert.AreEqual(5000, mem.ToArray().Count(x => x == 0));

                db.Storage.Delete("my/foto1.jpg");

                Dump.Pages(db, "deleted file");

            }
        }

        [TestMethod]
        public void Files_Store_My_Picture()
        {
            using (var db = new LiteEngine(dbpath))
            {
                var files = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Pictures", "*.jpg", SearchOption.AllDirectories);

                foreach (var f in files.Take(50))
                {
                    db.Storage.Upload(Path.GetFileName(f), f);
                }
            }

            using (var db = new LiteEngine(dbpath))
            {
                Directory.CreateDirectory(@"C:\temp\pictures-50");

                foreach (var f in db.Storage.All())
                {
                    f.SaveAs(db, @"C:\temp\pictures-50\" + f.Key, true);
                }

                var delete5 = db.Storage.All().Take(5);

                foreach(var f in delete5)
                    db.Storage.Delete(f.Key);

                Directory.CreateDirectory(@"C:\temp\pictures-45");

                foreach (var f in db.Storage.All())
                {
                    Debug.Print(f.Key);
                    f.SaveAs(db, @"C:\temp\pictures-45\" + f.Key, true);
                }
            }
        }
    }
}
