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
    /// Service for restore datafile with there a problem when save on disk
    /// </summary>
    internal class RedoService
    {
        public const long FINISH_POSITION = 4000;

        private CacheService _cache;
        private RecoveryService _recovery;
        private bool _enabled;

        public RedoService(RecoveryService recovery, CacheService cache, bool enabled)
        {
            _recovery = recovery;
            _cache = cache;
            _enabled = enabled;
        }

        public void CheckRedoFile(DiskService disk)
        {
            if (!_enabled) return;

            if (File.Exists(_recovery.RedoFile))
            {
                disk.UnLock();
                throw new LiteDBException("Redo file detected. Try reopen data file");
            }
        }

        public void CreateRedoFile()
        {
            if (!_enabled) return;

            if(File.Exists(_recovery.RedoFile))
                throw new LiteDBException("Redo file detected. Try reopen data file");

            // first, write all dirty pages, in sequence, in a .redo file
            using (var stream = File.Create(_recovery.RedoFile, BasePage.PAGE_SIZE))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    uint index = 0;

                    // always write header page
                    this.WritePage(index, writer, _cache.Header);

                    // write all dirty pages in sequence
                    foreach (var page in _cache.GetDirtyPages())
                    {
                        this.WritePage(++index, writer, page);
                    }

                    // mark header as finish
                    writer.Seek(FINISH_POSITION);

                    writer.Write(true); // mark as 1
                }
            }
        }

        /// <summary>
        /// Write a page in sequence, not in absolute position
        /// </summary>
        private void WritePage(uint index, BinaryWriter writer, BasePage page)
        {
            // Position cursor
            writer.Seek(index * BasePage.PAGE_SIZE);

            // target = it's the target position after write header. It's used when header does not conaints all PAGE_HEADER_SIZE
            var target = writer.BaseStream.Position + BasePage.PAGE_HEADER_SIZE;

            // Write page header
            page.WriteHeader(writer);

            // position writer to the end of page header
            writer.BaseStream.Seek(target - writer.BaseStream.Position, SeekOrigin.Current);

            // write content except for empty pages
            if (page.PageType != PageType.Empty)
            {
                page.WriteContent(writer);
            }
        }

        public void DeleteRedoFile()
        {
            if (!_enabled) return;

            // just delete recovery file, main datafile is consist
            File.Delete(_recovery.RedoFile);
        }
    }
}
