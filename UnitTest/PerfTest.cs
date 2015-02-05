using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class PerfTest
    {
        [TestMethod]
        public void Perf_Test()
        {
            var path = DB.Path(true, "test.db");

            using (var db = new LiteEngine("journal=true;filename=" + path))
            {
                db.BeginTrans();
                var col = db.GetCollection<Post>("posts");
                col.Insert(Post.GetData(20000));
                db.Commit();
            }
        }

        [TestMethod]
        public void PerfFile_Test()
        {
            var path = DB.Path(true, "test.db");

            using (var db = new LiteEngine("journal=false;filename=" + path))
            {
                var bytes = new byte[150 * 1024 * 1024];

                using (var m = new MemoryStream(bytes))
                {
                    db.FileStorage.Upload("myfile", m);
                }
            }
        }

        [TestMethod]
        public void PerfWriteFile_Test()
        {
            var path = DB.Path(true, "test.db");

            //var bytes = new byte[150 * 1024 * 1024];
            var bytes = 150 * 1024 * 1024;

            using (var f = File.Create(path))
            {
                f.SetLength(bytes);

                using (var w = new BinaryWriter(f))
                {

                    for (var i = 0; i < bytes; i = i + 8)
                    {
                        w.Write(new byte[8]);
                    }
                }
            }

        }
    }
}
