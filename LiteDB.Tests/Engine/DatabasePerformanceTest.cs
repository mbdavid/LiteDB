using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests
{
    [TestClass]
    public class DatabasePerformanceTest : TestBase
    {
        [TestMethod]
        public void Create_300k_Rows_DB_And_Search()
        {
            using (var tmp = new TempFile("journal=false"))
            {
                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    var c = db.GetCollection<PerfItem>("perf");
                    //c.EnsureIndex("MyGuid", true);
                    var id = 0;

                    for (var j = 0; j < 3; j++)
                    {
                        var d = DateTime.Now;
                        using (var trans = db.BeginTrans())
                        {

                            for (var i = 0; i < 100000; i++)
                            {
                                id++;

                                c.Insert(new PerfItem { Id = id, MyGuid = Guid.NewGuid(), Nome = "Jose Silva " + id });
                            }

                            trans.Commit();
                        }

                        Debug.WriteLine("Commits " + j + " in " + DateTime.Now.Subtract(d).TotalMilliseconds);
                    }
                }

                Guid g;

                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    var c = db.GetCollection<PerfItem>("perf");

                    //c.EnsureIndex("Id");

                    Debug.WriteLine("Total rows in collection " + c.Count());

                    var i = c.FindById(7737);

                    g = i.MyGuid;

                    Debug.WriteLine(i.MyGuid + " - " + i.Nome);
                }

                using (var db = new LiteDatabase(tmp.ConnectionString))
                {
                    var c = db.GetCollection<PerfItem>("perf");

                    var i = c.FindOne(Query.EQ("MyGuid", g));

                    Debug.WriteLine(i.MyGuid + " - " + i.Nome);
                }
            }
        }
    }

    public class PerfItem
    {
        public int Id { get; set; }
        public Guid MyGuid { get; set; }
        public string Nome { get; set; }
    }
}