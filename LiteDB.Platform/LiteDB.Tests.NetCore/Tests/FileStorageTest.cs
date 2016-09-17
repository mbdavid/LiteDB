
using System;
using System.IO;
using LiteDB.Tests.NetCore;
using LiteDB.Tests.NetCore.Tests;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace LiteDB.Tests
{
    public class FileStorage_Test : TestBase
    {
        public void FileStorage_InsertDelete()
        {
            var test_name = "FileStorage_InsertDelete";
            using (var dll = new TempFile(null, "dll"))
            {
                // create a dump file
                TestPlatform.FileWriteAllText(dll.Filename, "FileCoreContent");

                using (var db = new LiteDatabase(new MemoryStream()))
                {
                    // upload
                    db.FileStorage.Upload("Core.dll", dll.Filename);

                    // exits
                    var exists = db.FileStorage.Exists("Core.dll");
                    Helper.AssertIsTrue(test_name, 0, true == exists);

                    // find
                    var files = db.FileStorage.Find("Core");
                    Helper.AssertIsTrue(test_name, 1, 1 == files.Count());
                    Helper.AssertIsTrue(test_name, 2, "Core.dll" == files.First().Id);

                    // find by id
                    var core = db.FileStorage.FindById("Core.dll");
                    Helper.AssertIsTrue(test_name, 3, core != null);
                    Helper.AssertIsTrue(test_name, 4, "Core.dll" == core.Id);

                    // download
                    var mem = new MemoryStream();
                    db.FileStorage.Download("Core.dll", mem);
                    var content = Encoding.UTF8.GetString(mem.ToArray(), 0, (int)mem.Length);
                    Helper.AssertIsTrue(test_name, 5, "FileCoreContent" == content);

                    // delete
                    var deleted = db.FileStorage.Delete("Core.dll");
                    Helper.AssertIsTrue(test_name, 6, true == deleted);

                    // not found deleted
                    var deleted2 = db.FileStorage.Delete("Core.dll");
                    Helper.AssertIsTrue(test_name, 7, false == deleted2);
                }
            }
        }

        public void FileStorage_50files()
        {
            var test_name = "FileStorage_50files";
            var file5mb = new byte[5 * 1024 * 1024];

            using (var tmp = new TempFile())
            {
                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    for (var i = 0; i < 50; i++)
                    {
                        db.FileStorage.Upload("file_" + i, new MemoryStream(file5mb));
                    }
                }

                // filedb must have, at least, 250mb
                Helper.AssertIsTrue(test_name, 1, TestPlatform.GetFileSize(tmp.Filename) > (250 * 1024 * 1024));

                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    foreach (var f in db.FileStorage.FindAll())
                    {
                        using (var ftmp = new TempFile(null, "dll"))
                        {
                            f.SaveAs(ftmp.Filename);
                        }
                    }
                }
            }
        }
    }
}