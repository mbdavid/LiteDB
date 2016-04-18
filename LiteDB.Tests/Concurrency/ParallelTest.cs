using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Tests
{
    public class Target
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonIndex(true)]
        public string Name { get; set; }

        public DateTime LastUpdateCheck { get; set; }
    }

    [TestClass]
    public class ParallelTest
    {
        private static string _filename;

        [TestMethod]
        public void ParallelReadInsertUpdate_Test()
        {
            this.Setup();

            var update = Execute("u", db =>
            {
                var target = db.GetCollection<Target>("targets").FindOne(x => x.Name != null);
                if (target != null)
                {
                    target.LastUpdateCheck = DateTime.Now;
                    db.GetCollection<Target>("targets").Update(target);
                }
            });
            var insert = Execute("i", db => db.GetCollection<Target>("targets").Insert(this.CreateTarget()));
            var read = Execute("r", db => db.GetCollection<Target>("targets").FindAll().ToList());
            Task.WaitAll(update, insert, read);
        }

        [TestMethod]
        public void ParallelReadInsert_Test()
        {
            this.Setup();

            var insert = Execute("i", db => db.GetCollection<Target>("targets").Insert(this.CreateTarget()));
            var read = Execute("r", db => db.GetCollection<Target>("targets").FindAll().ToList());
            Task.WaitAll(insert, read);
        }

        [TestMethod]
        public void ParallelReadUpdate_Test()
        {
            this.Setup();

            var update = Execute("u", db =>
            {
                var target = db.GetCollection<Target>("targets").FindOne(x => x.Name != null);
                if (target != null)
                {
                    target.LastUpdateCheck = DateTime.Now;
                    db.GetCollection<Target>("targets").Update(target);
                }
            });
            var read = Execute("r", db => db.GetCollection<Target>("targets").FindAll().ToList());
            Task.WaitAll(update, read);
        }

        private Target CreateTarget()
        {
            return new Target { Name = Guid.NewGuid().ToString(), LastUpdateCheck = DateTime.Now };
        }

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            _filename = DB.RandomFile();
        }

        private void Setup()
        {
            using (var db = new LiteDatabase(_filename))
            {
                db.DropCollection("targets");
                for (var i = 0; i < 1000; i++)
                {
                    db.GetCollection<Target>("targets").Insert(this.CreateTarget());
                }
            }
        }

        private Task<ParallelLoopResult> Execute(string prefix, Action<LiteDatabase> action)
        {
            return Task.Factory.StartNew(() => Parallel.For(0, 100, x =>
            {
                //Console.Write($"{prefix}-{x}-{Thread.CurrentThread.ManagedThreadId} ");
                try
                {
                    using (var db = new LiteDatabase(_filename))
                    {
                        action(db);
                    }
                }
                catch (Exception e)
                {
                    Assert.Fail(e.Message);
                }
            }));
        }
    }
}