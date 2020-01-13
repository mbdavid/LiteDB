using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.IO;
using LiteDB;

namespace LiteDB.Demo
{
    class EntityA
    {
        public long Id { get; set; }
        public int X { get; set; }
        public string Y { get; set; }
    }

    public class InitConcurrentTest
    {
        private static long _idValue = 0;

        public static async Task TestManyThread()
        {
            const int TOTAL_NUM = 10000;
            const int TOTAL_TASKS = 2;

            // ensure using initialized database.
            if (File.Exists("manythread.db")) File.Delete("manythread.db");
            if (File.Exists("manythread-log.db")) File.Delete("manythread-log.db");

            using (var db = new LiteDB.LiteDatabase("manythread.db"))
            {
                await Task.WhenAll(
                    Enumerable.Range(0, TOTAL_TASKS).Select(async (idx) =>
                    {
                        // concurrent insert task
                        await Task.Yield();
                        db.BeginTrans();
                        var collection = db.GetCollection<EntityA>("HogeCollection");
                        for (int i = 0; i < TOTAL_NUM / TOTAL_TASKS; i++)
                        {
                            collection.Insert(new EntityA()
                            {
                                Id = Interlocked.Increment(ref _idValue),
                                X = idx * 10000 + i,
                                Y = $"{idx}_{i}"
                            });
                        }
                        db.Commit();
                    })
                ).ConfigureAwait(false);
            }
        }
    }
}