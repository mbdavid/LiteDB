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

                var f = db.Files.FindByKey("my/foto1.jpg");

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

    }
}
