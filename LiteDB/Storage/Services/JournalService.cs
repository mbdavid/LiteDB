using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LiteDB
{
    /// <summary>
    /// Service to create a journal file to garantee write operations will be atomic
    /// </summary>
    internal class JournalService
    {
        public const long FINISH_POSITION = BasePage.PAGE_SIZE - 1; // Last byte from header

        private CacheService _cache;
        private ConnectionString _connectionString;

        public JournalService(ConnectionString connectionString, CacheService cache)
        {
            _connectionString = connectionString;
            _cache = cache;
        }

        public void CreateJournalFile(Action action)
        {
            if (!_connectionString.JournalEnabled)
            {
                action();
                return;
            }

            var journal = JournalService.GetJournalFilename(_connectionString, true);

            // create journal file in EXCLUSIVE mode
            using (var stream = new FileStream(journal, 
                FileMode.Create, FileAccess.ReadWrite, FileShare.None, BasePage.PAGE_SIZE))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // first, allocate all journal file
                    var total = (uint)_cache.GetDirtyPages().Count();
                    stream.SetLength(total * BasePage.PAGE_SIZE);

                    // write all dirty pages in sequence on journal file
                    foreach (var page in _cache.GetDirtyPages())
                    {
                        this.WritePageInJournal(writer, page);
                    }

                    // flush all data
                    writer.Flush();

                    // mark header as finish
                    stream.Seek(FINISH_POSITION, SeekOrigin.Begin);

                    writer.Write(true); // mark as TRUE

                    // flush last finish mark
                    writer.Flush();

                    action();
                }

                stream.Dispose();

                File.Delete(journal);
            }
        }

        /// <summary>
        /// Write a page in sequence, not in absolute position
        /// </summary>
        private void WritePageInJournal(BinaryWriter writer, BasePage page)
        {
            // no need position cursor - journal writes in sequence
            var stream = writer.BaseStream;
            var posStart = stream.Position;
            var posEnd = posStart + BasePage.PAGE_SIZE;

            // Write page header
            page.WriteHeader(writer);

            // write content except for empty pages
            if (page.PageType != PageType.Empty)
            {
                page.WriteContent(writer);
            }

            // write with zero non-used page
            writer.Write(new byte[posEnd - stream.Position]);
        }

        /// <summary>
        /// Get a new journal file to write or check if exits one. If exists, test durint timeout - can be another process deleting
        /// </summary>
        public static string GetJournalFilename(ConnectionString connectionString, bool newFile)
        {
            var dir = Path.GetDirectoryName(connectionString.Filename);
            var filename = Path.GetFileNameWithoutExtension(connectionString.Filename) + "-journal";
            var ext = Path.GetExtension(connectionString.Filename);

            var journal = Path.Combine(dir, filename + ext);

            if (newFile)
            {
                var timeout = DateTime.Now.Add(connectionString.Timeout);

                while (DateTime.Now < timeout)
                {
                    if (!File.Exists(journal))
                    {
                        return journal;
                    }

                    System.Threading.Thread.Sleep(250);
                }

                throw LiteException.JournalFileFound(journal);
            }
            else
            {
                return File.Exists(journal) ? journal : null;
            }
        }
    }
}
