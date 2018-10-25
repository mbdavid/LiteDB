using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Manage linear memory segments to avoid re-create array buffer in heap memory
    /// ThreadSafe
    /// </summary>
    internal class MemoryStore : IDisposable
    {
        private readonly Dictionary<long, PageBuffer> _cleanPages = new Dictionary<long, PageBuffer>();

        private readonly List<PageBuffer> _writablePages = new List<PageBuffer>();

        /// <summary>
        /// Empty pages in store. If request page and has no more in store, do extend to create more memory allocation
        /// </summary>
        private readonly ConcurrentQueue<PageBuffer> _store = new ConcurrentQueue<PageBuffer>();

        public MemoryStore()
        {
            this.Extend();
        }

        /// <summary>
        /// Request a readonly page - this page can be shared with all only for read (will never be dirty)
        /// Each request increase share counter
        /// </summary>
        public PageBuffer GetReadablePage(long position, Action<long, ArraySlice<byte>> factory)
        {
            DEBUG(_writablePages.Any(x => x.Position == position), "this page already in writable list");

            if (_cleanPages.TryGetValue(position, out var page))
            {
                Interlocked.Increment(ref page.ShareCounter);
            }
            else
            {
                // get a clean page from store and read data from stream
                page = this.GetPageFromStore();

                page.Position = position;
                page.ShareCounter = 1;

                factory(position, page);
            }

            return page;
        }

        /// <summary>
        /// Request for a writable page - no other can read this page and this page has no reference
        /// Writable pages can be write or just released (with no write)
        /// </summary>
        public PageBuffer GetWritablePage(long position, Action<long, ArraySlice<byte>> factory)
        {
            DEBUG(_writablePages.Any(x => x.Position == position), "this page already in dirty list");

            // write pages always contains a new buffe
            var page = this.NewPage();

            // if request page already in clean list, just copy buffer and avoid load from stream
            if (_cleanPages.TryGetValue(position, out var clean))
            {
                Buffer.BlockCopy(clean.Array, clean.Offset, page.Array, page.Offset, PAGE_SIZE);
            }
            else
            {
                factory(position, page);
            }

            return page;
        }

        public PageBuffer NewPage()
        {
            var page = this.GetPageFromStore();

            _writablePages.Add(page);

            page.IsWritable = true;
            page.ShareCounter = 1;

            return page;
        }

        /// <summary>
        /// When page enter in queue to write, this page will be avaiable to read
        /// </summary>
        public void MarkAsClean(PageBuffer page)
        {
            DEBUG(page.IsWritable == false, "only writable pages can be marked as clean");
            DEBUG(page.Position == long.MaxValue, "page position must be defined");
            DEBUG((_cleanPages.GetOrDefault(page.Position)?.ShareCounter ?? 1) == 0, "if page already in clean list MUST are not in use by any other thread to read");

            // add (or update) page in clean list
            _cleanPages[page.Position] = page;

            // remove from writable pages and mark page as not writable anymore
            _writablePages.Remove(page);

            page.ShareCounter++;
        }

        /// <summary>
        /// After use a page (for read/write) must return this page into store
        /// </summary>
        public void ReturnPage(PageBuffer page)
        {
            DEBUG(page.ShareCounter <= 0, "pages must contains share counter when return to store");

            Interlocked.Decrement(ref page.ShareCounter);

            if (page.ShareCounter == 0)
            {
                // posso nesse momento decidir se esta pagina volta pra store ou fica disponivel
                // para novas leituras - ver conforme tamanho da store + memoria usada
            }
        }

        private PageBuffer GetPageFromStore()
        {
            if (_store.TryDequeue(out var page))
            {
                return page;
            }
            else
            {
                this.Extend();

                return this.GetPageFromStore();
            }
        }

        /// <summary>
        /// Create new linar buffer (byte[]) in heap and get slices using PageBuffer (ArraySlice). Each array segment contains one PAGE_SIZE
        /// All buffer will be allocated into G2 because has more than 85k
        /// </summary>
        private void Extend()
        {
            // lock store to ensure only 1 extend per time
            lock(_store)
            {
                if (_store.Count > 0) return;

                // create big linear array in heap (G2 - > 85Kb)
                var buffer = new byte[PAGE_SIZE * MEMORY_SEGMENT_SIZE];

                // slit linear array into many array slices
                for (var i = 0; i < MEMORY_SEGMENT_SIZE; i++)
                {
                    _store.Enqueue(new PageBuffer(buffer, i * PAGE_SIZE));
                }
            }
        }

        public void Dispose()
        {
        }
    }
}