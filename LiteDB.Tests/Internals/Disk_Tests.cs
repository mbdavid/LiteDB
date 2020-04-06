using System.IO;
using System.Collections.Generic;
using FluentAssertions;
using LiteDB.Engine;
using Xunit;
using System.Threading.Tasks;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Internals
{
    public class Disk_Tests
    {
        [Fact]
        public void Disk_Read_Write()
        {
            var settings = new EngineSettings
            {
                DataStream = new MemoryStream()
            };

            var disk = new DiskService(settings, new int[] { 10 });
            var pages = new List<PageBuffer>();

            // let's create 100 pages with 0-99 full data
            for (var i = 1; i <= 100; i++)
            {
                var p = disk.Cache.NewPage();

                p.Fill((byte) i); // fills with 1 - 100

                p.Write(i, 0); // but fix a valid pageID

                pages.Add(p);
            }

            // page will be saved in LOG file in PagePosition order (0-99)
            disk.WriteAsync(pages);

            // wait for async queue writes
            disk.Queue.Wait();

            // after release, no page can be read/write
            pages.Clear();

            // lets do some read tests
            var reader = disk.GetReader();

            for (var i = 1; i <= 100; i++)
            {
                var p = reader.ReadPage(i * 8192, false);

                p.Slice(4, PAGE_SIZE - 4).All((byte) i).Should().BeTrue(); // remove pageID 4 first bytes

                p.Release();
            }

            // test cache in use
            disk.Cache.PagesInUse.Should().Be(0);

            // wait all async threads
            disk.Dispose();
        }

        [Fact]
        public Task Disk_ExclusiveScheduler_Write() => Task.Factory.StartNew(Disk_Read_Write,
            CancellationToken.None, TaskCreationOptions.DenyChildAttach,
            new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler);
    }
}