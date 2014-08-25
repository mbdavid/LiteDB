using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LiteDB
{
    internal class DiskService : IDisposable
    {
        private ConnectionString _connectionString;

        private BinaryReader _reader;
        private BinaryWriter _writer;

        public DiskService(ConnectionString connectionString)
        {
            _connectionString = connectionString;

            // Open file as ReadOnly - if we need use Write, re-open in Write Mode
            var stream = File.Open(_connectionString.Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            _reader = new BinaryReader(stream);
        }

        /// <summary>
        /// Create a new Page instance and read data from disk
        /// </summary>
        public T ReadPage<T>(uint pageID)
            where T : BasePage, new()
        {
            // Position cursor
            _reader.Seek(pageID * BasePage.PAGE_SIZE);

            // Create page instance and read from disk (read page header + content page)
            var page = new T();

            // target = it's the target position after reader header. It's used when header does not conaints all PAGE_HEADER_SIZE
            var target = _reader.BaseStream.Position + BasePage.PAGE_HEADER_SIZE;

            // read page header
            page.ReadHeader(_reader);

            // read page content if page is not empty
            if (page.PageType != PageType.Empty)
            {
                // position reader to the end of page header
                _reader.BaseStream.Seek(target - _reader.BaseStream.Position, SeekOrigin.Current);

                // read page content
                page.ReadContent(_reader);
            }

            return page;
        }

        /// <summary>
        /// Write a page from memory to disk 
        /// </summary>
        public void WritePage(BasePage page)
        {
            DiskService.WritePage(GetWriter(), page);
        }

        /// <summary>
        /// Static method for write a page using a diferent writer - used when create empty datafile
        /// </summary>
        public static void WritePage(BinaryWriter writer, BasePage page)
        {
            // Position cursor
            writer.Seek(page.PageID * BasePage.PAGE_SIZE);

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

            // if page is dirty, clean up
            page.IsDirty = false;
        }

        /// <summary>
        /// Get BinaryWriter
        /// </summary>
        private BinaryWriter GetWriter()
        {
            // If no writer - re-open file in Write Mode
            if (_writer == null)
            {
                _reader.Close(); // Close reader

                var stream = File.Open(_connectionString.Filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

                _reader = new BinaryReader(stream);
                _writer = new BinaryWriter(stream);
            }

            return _writer;
        }

        #region Lock/Unlock functions

        /// <summary>
        /// Lock the datafile
        /// </summary>
        public void Lock()
        {
            var stream = GetWriter().BaseStream as FileStream;
            var autoResetEvent = new AutoResetEvent(false);
            var timeout = DateTime.Now.Add(_connectionString.Timeout);

            while (DateTime.Now < timeout)
            {
                try
                {
                    // try to lock - if is in use, a exception will be throwed
                    stream.Lock(0, 1);
                    return;
                }
                catch (IOException)
                {
                    // Watch the file waiting for changes. When change, try again
                    using (var w = new FileSystemWatcher(Path.GetDirectoryName(_connectionString.Filename), Path.GetFileName(_connectionString.Filename)))
                    {
                        w.EnableRaisingEvents = true;

                        w.Changed += (s, e) =>
                        {
                            autoResetEvent.Set();
                        };
                    }

                    autoResetEvent.WaitOne(_connectionString.Timeout);
                }
            }

            throw new ApplicationException("Connection Timeout");
        }

        /// <summary>
        /// Unlock the datafile
        /// </summary>
        public void UnLock()
        {
            var stream = GetWriter().BaseStream as FileStream;

            stream.Unlock(0, 1);
        }

        #endregion

        public void Dispose()
        {
            _reader.Close();

            if (_writer != null)
                _writer.Close();
        }
    }
}
