using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

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
                db.FileStorage.Upload("Core.dll", "Core.dll");

                var exists = db.FileStorage.Exists("Core.dll");

                Assert.AreEqual(true, exists);

                var deleted = db.FileStorage.Delete("Core.dll");

                Assert.AreEqual(true, deleted);

                var deleted2 = db.FileStorage.Delete("Core.dll");

                Assert.AreEqual(false, deleted2);
            }

            File.Delete("Core.dll");
        }
    }
}
