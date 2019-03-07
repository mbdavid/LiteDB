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

namespace LiteDB.Internals.Disk
{
    [TestClass]
    public class Cache_Tests
    {
        [TestMethod]
        public void Cache_Read_Write()
        {
            var m = new MemoryCache(1000, 1000);

            Assert.AreEqual(0, m.PagesInUse);

            var p0 = m.NewPage();

            // new pages are writables
            Assert.AreEqual(p0.ShareCounter, -1);

            // simulate write operation on page
            p0.Origin = FileOrigin.Log;
            p0.Position = 0;
            p0.Write(123, 10);

            Assert.AreEqual(1, m.PagesInUse);

            var readable = m.TryMoveToReadable(p0);

            // now, page are readable (but still in use)
            Assert.IsTrue(readable);
            Assert.AreEqual(1, p0.ShareCounter);

            // after save page in disk - release page
            p0.Release();

            Assert.AreEqual(0, p0.ShareCounter);

            // now get same page again
            var p1 = m.GetReadablePage(0, FileOrigin.Log, (p, s) => { });

            Assert.AreEqual(123, p1.ReadInt32(10));
            Assert.AreEqual(1, p1.ShareCounter);

            // let's read again (must return same instance but increment share counter)
            var p2 = m.GetReadablePage(0, FileOrigin.Log, (p, s) => { });

            Assert.AreSame(p1, p2);

            Assert.AreEqual(2, p2.ShareCounter);

            // releasing first
            p1.Release();

            Assert.AreEqual(1, p2.ShareCounter);

            // releasing second
            p2.Release();

            Assert.AreEqual(0, m.PagesInUse);
        }

        [TestMethod]
        public void Cache_Segments()
        {
            var m = new MemoryCache();

            // in ctor, memory cache create only 1 memory segment
            Assert.AreEqual(1, m.ExtendSegments);

            var pages = new List<PageBuffer>();

            // now, let's request for 2001 pages to create more 2 segments
            for(var i = 0; i < (Constants.MEMORY_SEGMENT_SIZE * 2) + 1; i++)
            {
                pages.Add(m.NewPage());
            }

            Assert.AreEqual(3, m.ExtendSegments);

            // now, release 50% from at minimum cache reuse
            foreach(var page in pages.Take(Constants.MINIMUM_CACHE_REUSE / 2))
            {
                m.TryMoveToReadable(page);
                page.Release();
            }

        }
    }
}