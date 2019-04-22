using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using System.Threading;

namespace LiteDB.Internals
{
    [TestClass]
    public class Disk_Tests
    {
        [TestMethod]
        public void Disk_Read_Write()
        {
            var settings = new EngineSettings
            {
                MemorySegmentSize = 10,
                DataStream = new MemoryStream(),
                LogStream = new MemoryStream()
            };

            var disk = new DiskService(settings);
            var pages = new List<PageBuffer>();

            // let's create 100 pages with 0-99 full data
            for (var i = 0; i < 100; i++)
            {
                var p = disk.NewPage();

                p.Fill((byte)i); // fills with 0 - 99

                pages.Add(p);
            }

            // page will be saved in LOG file in PagePosition order (0-99)
            disk.WriteAsync(pages);

            // after release, no page can be read/write
            pages.Clear();

            // lets do some read tests
            var reader = disk.GetReader();

            for (var i = 0; i < 100; i++)
            {
                var p = reader.ReadPage(i * 8192, false, FileOrigin.Log);

                Assert.IsTrue(p.All((byte)i));

                p.Release();
            }

            // test cache in use
            Assert.AreEqual(0, disk.Cache.PagesInUse);

            // wait all async threads
            disk.Dispose();
        }
    }
}