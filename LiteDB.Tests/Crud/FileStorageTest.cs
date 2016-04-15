using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    [TestClass]
    public class FileStorage_Test
    {
        [TestMethod]
        public void FileStorage_InsertDelete()
        {
            // create a dump file
            File.WriteAllText("Core.dll", "FileCoreContent");

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                // upload
                db.FileStorage.Upload("Core.dll", "Core.dll");

                // exits
                var exists = db.FileStorage.Exists("Core.dll");
                Assert.AreEqual(true, exists);

                // find
                var files = db.FileStorage.Find("Core");
                Assert.AreEqual(1, files.Count());
                Assert.AreEqual("Core.dll", files.First().Id);

                // find by id
                var core = db.FileStorage.FindById("Core.dll");
                Assert.IsNotNull(core);
                Assert.AreEqual("Core.dll", core.Id);

                // download
                var mem = new MemoryStream();
                db.FileStorage.Download("Core.dll", mem);
                var content = Encoding.UTF8.GetString(mem.ToArray());
                Assert.AreEqual("FileCoreContent", content);

                // delete
                var deleted = db.FileStorage.Delete("Core.dll");
                Assert.AreEqual(true, deleted);

                // not found deleted
                var deleted2 = db.FileStorage.Delete("Core.dll");
                Assert.AreEqual(false, deleted2);
            }

            File.Delete("Core.dll");
        }

        [TestMethod]
        public void FileStoage_50files()
        {
            var file5mb = new byte[5 * 1024 * 1024];
            var filedb = DB.RandomFile();

            using (var db = new LiteDatabase(filedb))
            {
                for(var i = 0; i < 50; i++)
                {
                    db.FileStorage.Upload("file_" + i, new MemoryStream(file5mb));
                }
            }

            // filedb must have, at least, 250mb
            Assert.IsTrue(new FileInfo(filedb).Length > (250 * 1024 * 1024), "Datafile must have more than 250Mb");

            using (var db = new LiteDatabase(filedb))
            {
                foreach(var f in db.FileStorage.FindAll())
                {
                    f.SaveAs(DB.RandomFile("bin"));
                }
            }
        }
    }
}