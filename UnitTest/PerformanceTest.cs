using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LiteDB;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTest
{
    public class PerfItem
    {
        public int Id { get; set; }
        public Guid MyGuid { get; set; }
        public string Nome { get; set; }
    }

    [TestClass]
    public class PerformanceTest
    {
        private const string dbpath = @"filename=C:\Temp\perf.ldb;journal=true";

        [TestInitialize]
        public void Init()
        {
            //File.Delete(dbpath);
        }

        [TestMethod]
        public void Create_100k_Rows_DB()
        {
            using (var db = new LiteEngine(dbpath))
            {
                var c = db.GetCollection<PerfItem>("perf");
                //c.EnsureIndex("MyGuid", true);
                var id = 0;

                for (var j = 0; j < 3; j++)
                {
                    var d = DateTime.Now;
                    db.BeginTrans();

                    for (var i = 0; i < 10000; i++)
                    {
                        id++;

                        c.Insert(id, new PerfItem { Id = id, MyGuid = Guid.NewGuid(), Nome = "Jose Silva " + id });
                    }

                    db.Commit();
                    Debug.Print("Commits " + j + " in " + DateTime.Now.Subtract(d).TotalMilliseconds);
                }
            }
        }

        [TestMethod]
        public void Search_Perf()
        {
            Guid g;

            using (var db = new LiteEngine(dbpath))
            {
                var c = db.GetCollection<PerfItem>("perf");

                Debug.Print("Total rows in collection " + c.Count());

                var i = c.FindById(7737);

                g = i.MyGuid;

                Debug.Print(i.MyGuid + " - " + i.Nome);
            }

            using (var db = new LiteEngine(dbpath))
            {
                var c = db.GetCollection<PerfItem>("perf");

                var i = c.FindOne(Query.EQ("MyGuid", g));

                Debug.Print(i.MyGuid + " - " + i.Nome);
            }

        }

    }
}
