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
        public void Store_Files()
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

                var meta = new Dictionary<string, object>();
                meta["my-data"] = "Google LiteDB";

                db.Files.Store("my/foto1.jpg", new MemoryStream(new byte[5000]), meta);

                Dump.Pages(db ,"after file");

                var f = db.Files.FindById("my/foto1.jpg");

                Debug.Print("Size: " + f.Length);
                Debug.Print("Meta: " + f.Metadata["my-data"]);
                Debug.Print("Date: " + f.UploadDate);

                var mem = new MemoryStream();

                f.OpenRead(db).CopyTo(mem);

                Debug.Print("Size in mem: " + mem.Length);
                Debug.Print("All bytes is ZERO: " + mem.ToArray().Count(x => x == 0));

                db.Files.Delete("my/foto1.jpg");

                Dump.Pages(db, "deleted file");

            }
        }

        [TestMethod]
        public void Store_My_Picture()
        {
            using (var db = new LiteEngine(dbpath))
            {
                var files = Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Pictures", "*.jpg", SearchOption.AllDirectories);

                foreach (var f in files.Take(200))
                {
                    db.Files.Store(Path.GetFileName(f), f);
                }
            }

            using (var db = new LiteEngine(dbpath))
            {
                Directory.CreateDirectory(@"C:\temp\restore");

                foreach (var f in db.Files.All())
                {
                    Debug.Print(f.Id);
                    f.SaveAs(db, @"C:\temp\restore\" + f.Id, true);
                }

                var first5 = db.Files.All().Take(5);

                foreach(var f in first5)
                    db.Files.Delete(f.Id);

                Directory.CreateDirectory(@"C:\temp\restore2");

                foreach (var f in db.Files.All())
                {
                    Debug.Print(f.Id);
                    f.SaveAs(db, @"C:\temp\restore2\" + f.Id, true);
                }

            }

        }

    }
}
