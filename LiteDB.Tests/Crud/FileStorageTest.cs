using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    }
}