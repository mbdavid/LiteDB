using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace LiteDB.Tests
{
    [TestClass]
    public class FileStorage_Test : TestBase
    {
        [TestMethod]
        public void FileStorage_InsertDelete()
        {
            // create a dump file
            var coreDllPath = TestPlatform.FileWriteAllText("Core.dll", "FileCoreContent");

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                // upload
                db.FileStorage.Upload("Core.dll", coreDllPath);

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
                var content = Encoding.UTF8.GetString(mem.ToArray(), 0, (int)mem.Length);
                Assert.AreEqual("FileCoreContent", content);

                // delete
                var deleted = db.FileStorage.Delete("Core.dll");
                Assert.AreEqual(true, deleted);

                // not found deleted
                var deleted2 = db.FileStorage.Delete("Core.dll");
                Assert.AreEqual(false, deleted2);
            }

            TestPlatform.DeleteFile("Core.dll");
        }

        [TestMethod]
        public void FileStoage_50files()
        {
            var file5mb = new byte[5 * 1024 * 1024];
            var filedb = DB.RandomFile();

            using (var db = new LiteDatabase(filedb))
            {
                for (var i = 0; i < 50; i++)
                {
                    db.FileStorage.Upload("file_" + i, new MemoryStream(file5mb));
                }
            }

            // filedb must have, at least, 250mb
            Assert.IsTrue(TestPlatform.GetFileSize(filedb) > (250 * 1024 * 1024), "Datafile must have more than 250Mb");

            var binFiles = new List<string>();

            using (var db = new LiteDatabase(filedb))
            {
                foreach (var f in db.FileStorage.FindAll())
                {
                    var file = DB.RandomFile("bin");
                    binFiles.Add(file);
                    f.SaveAs(file);
                }
            }

            TestPlatform.DeleteFile(filedb);

            foreach (var f in binFiles)
            {
                TestPlatform.DeleteFile(f);
            }
        }

    }
}