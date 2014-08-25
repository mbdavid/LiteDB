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
    internal class RecoveryService
    {
        const int ERROR_SHARING_VIOLATION = 32;
        const int ERROR_LOCK_VIOLATION = 33;

        private ConnectionString _connectionString;

        public string RedoFile { get; private set; }

        public RecoveryService(ConnectionString connectionString)
        {
            _connectionString = connectionString;

            this.RedoFile = Path.ChangeExtension(_connectionString.Filename, ".redo");
        }

        public void TryRecovery()
        {
            // no recovery file, nothing to do
            if (!File.Exists(this.RedoFile)) return;

            // check if file is not in use
            this.IsFileInUse(this.RedoFile, (stream) =>
            {
                // check if FINISH_POSITON is true
                using (var reader = new BinaryReader(stream))
                {
                    reader.Seek(RedoService.FINISH_POSITION);

                    // if not finish, datafile is intact and no chances are comited
                    if (reader.ReadBoolean() == true)
                    {
                        this.DoRecovery(reader);

                        // work done. datafile is correct again
                        return;
                    }
                }
            });

            // redo file exisits, but is invalid. just deleted
            File.Delete(this.RedoFile);
        }

        private void DoRecovery(BinaryReader reader)
        {
            // open disk service
            using (var disk = new DiskService(_connectionString))
            {
                disk.Lock();

                uint index = 0;

                // while pages, read from redo, write on disk
                while (reader.PeekChar() >= 0)
                {
                    var page = this.ReadPage(index++, reader);

                    disk.WritePage(page);
                }

                reader.Close();

                File.Delete(this.RedoFile);

                disk.UnLock();
            }
        }

        private BasePage ReadPage(uint index, BinaryReader reader)
        {
            // Position cursor
            reader.Seek(index * BasePage.PAGE_SIZE);

            // Create page instance and read from disk (read page header + content page)
            var page = new BasePage();

            // target = it's the target position after reader header. It's used when header does not conaints all PAGE_HEADER_SIZE
            var target = reader.BaseStream.Position + BasePage.PAGE_HEADER_SIZE;

            // read page header
            page.ReadHeader(reader);

            // Convert BasePage to correct Page Type
            if (page.PageType == PageType.Header) page = page.CopyTo<HeaderPage>();
            else if (page.PageType == PageType.Collection) page = page.CopyTo<CollectionPage>();
            else if (page.PageType == PageType.Index) page = page.CopyTo<IndexPage>();
            else if (page.PageType == PageType.Data) page = page.CopyTo<DataPage>();
            else if (page.PageType == PageType.Extend) page = page.CopyTo<ExtendPage>();

            // read page content if page is not empty
            if (page.PageType != PageType.Empty)
            {
                // position reader to the end of page header
                reader.BaseStream.Seek(target - reader.BaseStream.Position, SeekOrigin.Current);

                // read page content
                page.ReadContent(reader);
            }

            return page;
        }

        private void IsFileInUse(string filename, Action<FileStream> fn)
        {
            // check if is using by another process, if not, call fn passing stream opened
            try
            {
                using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    fn(stream);
                }
            }
            catch (IOException exception)
            {
                int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
                if (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)
                {
                    // file in use by another process, do nothing
                    return;
                }
                throw exception;
            }
        }
    }
}
