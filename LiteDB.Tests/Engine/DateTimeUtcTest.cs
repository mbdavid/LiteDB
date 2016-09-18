using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    // from #184
    public class Sample
    {
        public DateTime Timestamp { get; set; }
        public int X { get; set; }
    }

    [TestClass]
    public class DateTimeUtcTest : TestBase
    {
        [TestMethod]
        public void DateTimeUtc_Test()
        {
            // see: http://stackoverflow.com/questions/6930489/safely-comparing-local-and-universal-datetimes

            var kind = DateTimeKind.Utc;

            // with EnsureIndex before
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var samples = db.GetCollection<Sample>("samples");

                samples.EnsureIndex("Timestamp");

                for (int i = 1; i <= 5; i++)
                {
                    var sample = new Sample
                    {
                        Timestamp = new DateTime(2016, 6, 1, i, 0, 0, kind),
                        X = i
                    };

                    samples.Insert(sample);
                }

                var doc = samples.FindOne(Query.EQ("Timestamp", new DateTime(2016, 6, 1, 1, 0, 0, kind)));

                Assert.IsNotNull(doc);
                Assert.AreEqual(1, doc.X);
            }

            // without EnsureIndex
            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var samples = db.GetCollection<Sample>("samples");

                for (int i = 1; i <= 5; i++)
                {
                    var sample = new Sample
                    {
                        Timestamp = new DateTime(2016, 6, 1, i, 0, 0, kind),
                        X = i
                    };

                    samples.Insert(sample);
                }

                var doc = samples.FindOne(Query.EQ("Timestamp", new DateTime(2016, 6, 1, 1, 0, 0, kind)));

                Assert.IsNotNull(doc);
                Assert.AreEqual(1, doc.X);
            }
        }
    }
}