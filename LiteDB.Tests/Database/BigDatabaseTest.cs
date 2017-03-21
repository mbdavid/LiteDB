using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using System.Diagnostics;
using System;

namespace LiteDB.Tests
{
    public class EndpointMatch
    {
        public int Id { get; set; }
        public string Endpoint { get; set; }
        public DateTime Timestamp { get; set; }
        public int FragLimit { get; set; }
        public int TimeLimit { get; set; }
        public double TimeElapsed { get; set; }
        public List<Scoreboard> Scoreboard { get; set; }
    }

    public class Scoreboard
    {
        public string Name { get; set; }
        public int Frags { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
    }

    // [TestClass]
    public class BigDatabaseTest
    {
        [TestMethod]
        public void BigDatabase_Test()
        {
            using (var file = new TempFile())
            {
                using (var db = new LiteDatabase(file.Filename))
                {
                    var data = db.GetCollection<EndpointMatch>("Matches");

                    data.InsertBulk(GetDocs(1, 400000, false));

                    GC.Collect();
                }

                using (var db = new LiteDatabase(file.Filename))
                {
                    var data = db.GetCollection<EndpointMatch>("Matches");

                    var sw = Stopwatch.StartNew();

                    data.Insert(GetDocs(400001, 400001, true).First());

                    sw.Stop();

                    var memoryUsed = GC.GetTotalMemory(false) / 1024 / 1024;
                    var timeSpend = sw.ElapsedMilliseconds;
                }
            }
        }

        private IEnumerable<EndpointMatch> GetDocs(int init, int end, bool newData)
        {
            var insertionData = new EndpointMatch();

            insertionData.Scoreboard = new List<Scoreboard>();

            for (int i = 0; i < 50; i++)
            {
                insertionData.Scoreboard.Add(new Scoreboard());
            }

            if (newData)
            {
                insertionData.TimeElapsed = 52;
                insertionData.Endpoint = "GoodByeMemory(";
            }

            for (int i = init; i <= end; i++)
            {
                insertionData.Id = i;
                yield return insertionData;
            }
        }
    }
}