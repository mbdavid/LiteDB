using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using LiteDB.Engine;
using Xunit;

namespace LiteDB.Internals
{
    public class Cache_Tests
    {
        [Fact]
        public void Cache_Read_Write()
        {
            var m = new MemoryCache(new int[] { 10 });

            m.PagesInUse.Should().Be(0);

            var p0 = m.NewPage();

            // new pages are writables
            (p0.ShareCounter).Should().Be(-1);

            // simulate write operation on page
            p0.Origin = FileOrigin.Log;
            p0.Position = 0;
            p0.Write(123, 10);

            m.WritablePages.Should().Be(1);

            var readable = m.TryMoveToReadable(p0);

            // now, page are readable
            readable.Should().BeTrue();
            p0.ShareCounter.Should().Be(0);

            // now get same page again
            var p1 = m.GetReadablePage(0, FileOrigin.Log, (p, s) => { });

            p1.ReadInt32(10).Should().Be(123);
            p1.ShareCounter.Should().Be(1);

            // let's read again (must return same instance but increment share counter)
            var p2 = m.GetReadablePage(0, FileOrigin.Log, (p, s) => { });

            p1.Should().Be(p2);

            p2.ShareCounter.Should().Be(2);

            // releasing first
            p1.Release();

            p2.ShareCounter.Should().Be(1);

            // releasing second
            p2.Release();

            m.PagesInUse.Should().Be(0);

            p0.ShareCounter.Should().Be(0);
            p1.ShareCounter.Should().Be(0);
            p2.ShareCounter.Should().Be(0);
        }

        [Fact]
        public void Cache_Extends()
        {
            var m = new MemoryCache(new int[] { 10 });
            var pos = 0;

            // in ctor, memory cache create only 1 memory segment
            m.ExtendSegments.Should().Be(1);

            var pages = new List<PageBuffer>();

            // request 17 pages to write in disk
            for (var i = 0; i < 17; i++)
            {
                pages.Add(m.NewPage());
            }

            m.ExtendSegments.Should().Be(2);

            // release 5 pages (this pages will be in readable-list)
            foreach (var p in pages.Take(5))
            {
                // simulate write
                p.Origin = FileOrigin.Log;
                p.Position = ++pos;

                m.TryMoveToReadable(p);
            }

            // checks if still 2 segments in memory (segments never decrease)
            m.ExtendSegments.Should().Be(2);

            // but only 3 free pages
            m.FreePages.Should().Be(3);

            // now, request new 5 pages to write
            for (var i = 0; i < 5; i++)
            {
                pages.Add(m.NewPage());
            }

            // extends must be increase
            m.ExtendSegments.Should().Be(3);

            // but if I release more than 10 pages, now I will re-use old pages
            foreach (var p in pages.Where(x => x.ShareCounter == -1).Take(10))
            {
                // simulate write
                p.Origin = FileOrigin.Log;
                p.Position = ++pos;

                m.TryMoveToReadable(p);
            }

            m.WritablePages.Should().Be(7);
            m.FreePages.Should().Be(8);

            // now, if I request for 10 pages, all pages will be reused (no segment extend)
            for (var i = 0; i < 10; i++)
            {
                pages.Add(m.NewPage());
            }

            // keep same extends
            m.ExtendSegments.Should().Be(3);

            // discard all pages
            PageBuffer pw;

            while ((pw = pages.FirstOrDefault(x => x.ShareCounter == -1)) != null)
            {
                m.DiscardPage(pw);
            }
        }
    }
}