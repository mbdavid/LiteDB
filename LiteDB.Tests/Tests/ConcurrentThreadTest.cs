#if !PCL
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB.Shell;
using System.Threading;
using System.Diagnostics;

namespace LiteDB.Tests
{
    //[TestClass]
    public class ConcurrentThreadTest : TestBase
    {
        private Random _rnd = new Random();
        private string _connectionString;
        private bool _running = false;

        [TestMethod]
        public void ConcurrentThread_Test()
        {
            using (var tmp = new TempFile())
            {
                _connectionString = tmp.ConnectionString;

                _running = true;

                for (var i = 0; i < 5; i++)
                {
                    var thread = new Thread(InsertWork);
                    thread.Start();
                }
                for (var i = 0; i < 5; i++)
                {
                    var thread = new Thread(ReadWork);
                    thread.Start();
                }

                var begin = DateTime.Now;

                while (_running)
                {
                    if (DateTime.Now >= begin.AddMinutes(1))
                    {
                        _running = false;
                        break;
                    }
                    Thread.Sleep(100);
                }

            }
        }

        void InsertWork()
        {
            var rnd = new Random(DateTime.Now.Millisecond);

            while (_running)
            {
                var doc = new BsonDocument()
                    .Add("name", rnd.Next().ToString())
                    .Add("age", rnd.Next(0, 80));

                using (var conn = new LiteDatabase(_connectionString))
                {
                    var col = conn.GetCollection("col");
                    col.Insert(doc);
                }

                Thread.Sleep(1000);
            }
        }

        void ReadWork()
        {
            var rnd = new Random(DateTime.Now.Millisecond);

            while (_running)
            {
                using (var conn = new LiteDB.LiteDatabase("test2.db"))
                {
                    var col = conn.GetCollection("col");
                    var docs = col.Find(Query.EQ("age", rnd.Next(0, 80))).ToArray();
                }

                Thread.Sleep(1000);
            }
        }
    }
}
#endif
