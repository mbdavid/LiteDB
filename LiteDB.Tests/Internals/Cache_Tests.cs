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
    public class Cache_Tests
    {
        [TestMethod]
        public void Cache_Read_Write()
        {
            var m = new MemoryCache(10);

            Assert.AreEqual(0, m.PagesInUse);

            var p0 = m.NewPage();

            // new pages are writables
            Assert.AreEqual(p0.ShareCounter, -1);

            // simulate write operation on page
            p0.Origin = FileOrigin.Log;
            p0.Position = 0;
            p0.Write(123, 10);

            Assert.AreEqual(1, m.WritablePages);

            var readable = m.TryMoveToReadable(p0);

            // now, page are readable
            Assert.IsTrue(readable);
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

            Assert.AreEqual(0, p0.ShareCounter);
            Assert.AreEqual(0, p1.ShareCounter);
            Assert.AreEqual(0, p2.ShareCounter);
        }

        [TestMethod]
        public void Cache_Extends()
        {
            var m = new MemoryCache(10);
            var pos = 0;

            // in ctor, memory cache create only 1 memory segment
            Assert.AreEqual(1, m.ExtendSegments);

            var pages = new List<PageBuffer>();

            // request 17 pages to write in disk
            for(var i = 0; i < 17 ; i++)
            {
                pages.Add(m.NewPage());
            }

            Assert.AreEqual(2, m.ExtendSegments);

            // release 5 pages (this pages will be in readable-list)
            foreach(var p in pages.Take(5))
            {
                // simulate write
                p.Origin = FileOrigin.Log;
                p.Position = ++pos;

                m.TryMoveToReadable(p);
            }

            // checks if still 2 segments in memory (segments never decrease)
            Assert.AreEqual(2, m.ExtendSegments);

            // but only 3 free pages
            Assert.AreEqual(3, m.FreePages);

            // now, request new 5 pages to write
            for(var i = 0; i < 5; i++)
            {
                pages.Add(m.NewPage());
            }

            // extends must be increase
            Assert.AreEqual(3, m.ExtendSegments);

            // but if I release more than 10 pages, now I will re-use old pages
            foreach (var p in pages.Where(x => x.ShareCounter == -1).Take(10))
            {
                // simulate write
                p.Origin = FileOrigin.Log;
                p.Position = ++pos;

                m.TryMoveToReadable(p);
            }

            Assert.AreEqual(7, m.WritablePages);
            Assert.AreEqual(8, m.FreePages);

            // now, if I request for 10 pages, all pages will be reused (no segment extend)
            for (var i = 0; i < 10; i++)
            {
                pages.Add(m.NewPage());
            }

            // keep same extends
            Assert.AreEqual(3, m.ExtendSegments);

        }
    }
}