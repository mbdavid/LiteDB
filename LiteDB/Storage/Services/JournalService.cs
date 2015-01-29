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
            using (var stream = File.Open(journal, 
                FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    // first, allocate all journal file
                    var total = (uint)_cache.GetDirtyPages().Count();
                    stream.SetLength(total * BasePage.PAGE_SIZE);

                    uint index = 0;

                    // for better performance, write first page at end of file and others in sequence order
                    foreach (var page in _cache.GetDirtyPages())
                    {
                        if (index == 0)
                        {
                            this.WritePageInJournal(total - 1, writer, page);
                        }
                        else
                        {
                            this.WritePageInJournal(index, writer, page);
                        }
                        index++;
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

                stream.Dispose();

                File.Delete(journal);
            }
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

        /// <summary>
        /// Get a new journal file to write or check if exits one. Append (index) if journal file exists (when OS do not deleted yet - check better OS support)
        /// </summary>
        public static string GetJournalFilename(ConnectionString connectionString, bool newFile)
        {
            var dir = Path.GetDirectoryName(connectionString.Filename);
            var filename = Path.GetFileNameWithoutExtension(connectionString.Filename) + "-journal";
            var ext = Path.GetExtension(connectionString.Filename);

            if (newFile)
            {
                //return Path.Combine(dir, filename + ext);
                var file = "";
                var index = 0;

                while (File.Exists(file = Path.Combine(dir, filename + (index > 0 ? index.ToString() : "") + ext)))
                {
                    index++;
                }

                return file;
            }
            else
            {
                //var p = Path.Combine(dir, filename + ext);
                //return File.Exists(p) ? p : null;
                var files = Directory.GetFiles(dir, filename + "*" + ext, SearchOption.TopDirectoryOnly);

                return files.Length > 0 ? files.Last() : null;
            }
        }
    }
}
