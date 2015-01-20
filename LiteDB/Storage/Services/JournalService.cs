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

            if (File.Exists(_connectionString.JournalFilename))
            {
                throw new LiteException("Journal file detected, transaction aborted. Try reopen datafile");
            }

            // create journal file in EXCLUSIVE mode
            using (var stream = File.Open(_connectionString.JournalFilename, 
                FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    uint index = 0;

                    // write all dirty pages in sequence
                    foreach (var page in _cache.GetDirtyPages())
                    {
                        this.WritePageInJournal(++index, writer, page);
                    }

                    // flush all data
                    writer.Flush();

                    // mark header as finish
                    writer.Seek(FINISH_POSITION);

                    writer.Write(true); // mark as TRUE

                    // flush last finish mark
                    writer.Flush();

                    action();
                }
            }

            this.DeleteJournalFile();
        }

        /// <summary>
        /// Write a page in sequence, not in absolute position
        /// </summary>
        private void WritePageInJournal(uint index, BinaryWriter writer, BasePage page)
        {
            // Position cursor
            writer.Seek(index * BasePage.PAGE_SIZE);

            // target = it's the target position after write header. It's used when header does not conaints all PAGE_HEADER_SIZE
            var target = writer.BaseStream.Position + BasePage.PAGE_HEADER_SIZE;

            // Write page header
            page.WriteHeader(writer);

            // write content except for empty pages
            if (page.PageType != PageType.Empty)
            {
                // position writer to the end of page header
                writer.BaseStream.Seek(target - writer.BaseStream.Position, SeekOrigin.Current);

                page.WriteContent(writer);
            }
        }

        public void DeleteJournalFile()
        {
            if (!_connectionString.JournalEnabled) return;

            // just delete journal file, main datafile is consist
            File.Delete(_connectionString.JournalFilename);
        }
    }
}
