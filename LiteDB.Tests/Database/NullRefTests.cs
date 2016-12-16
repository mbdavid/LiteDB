using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LiteDB.Tests.Database
{
    public class Pipeline
    {
        public int Id { get; set; }
        public List<Job> Jobs { get; set; } = new List<Job>();
    }

    public class Job
    {
        public int Id { get; set; }
    }

    [TestClass]
    public class NullRefTests
    {
        [TestMethod]
        public void DbRef_ToDeleted_ThrowsNullReferenceException()
        {
            var mapper = new BsonMapper();
            mapper.Entity<Pipeline>().DbRef(x => x.Jobs, "jobs");

            using (var db = new LiteDatabase(new MemoryStream(), mapper))
            {
                var pipelineCollection = db.GetCollection<Pipeline>("pipelines");
                var jobCollection = db.GetCollection<Job>("jobs");

                var pipeline = new Pipeline();
                pipelineCollection.Insert(pipeline);

                var job = new Job();
                jobCollection.Insert(job);

                pipeline.Jobs.Add(job);
                pipelineCollection.Update(pipeline);

                jobCollection.Delete(job.Id);

                var pipelines = db.GetCollection<Pipeline>("pipelines").Include(p => p.Jobs).FindAll().ToArray();
                Assert.AreEqual(1, pipelines.Length);

                pipeline = pipelines.Single();
                Assert.AreEqual(0, pipeline.Jobs.Count);
            }
        }
    }
}