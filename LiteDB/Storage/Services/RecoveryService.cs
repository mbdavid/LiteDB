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
        private ConnectionString _connectionString;

        public RecoveryService(ConnectionString connectionString)
        {
            _connectionString = connectionString;
        }

        public void TryRecovery()
        {
            var journal = JournalService.GetJournalFilename(_connectionString, false);

            // no journal file, nothing to do
            if (string.IsNullOrEmpty(journal)) return;

            // if I can open journal file, test FINISH_POSITION
            this.OpenExclusiveFile(journal, (stream) =>
            {
                // check if FINISH_POSITON is true
                using (var reader = new BinaryReader(stream))
                {
                    stream.Seek(JournalService.FINISH_POSITION, SeekOrigin.Begin);

                    // if file is finish, datafile needs to be recovery. if not,
                    // the failure ocurrs during write journal file but not finish - just discard it
                    if (reader.ReadBoolean() == true)
                    {
                        this.DoRecovery(reader);
                    }
                }

                // close stream for delete file
                stream.Close();

                File.Delete(journal);
            });

            // if I can't open, it's in use (and it's ok, there is a transaction executing in another process)
        }

        private void DoRecovery(BinaryReader reader)
        {
            // open disk service
            using (var disk = new DiskService(_connectionString))
            {
                disk.Lock();

                // while pages, read from redo, write on disk
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    var page = this.ReadPageJournal(reader);

                    disk.WritePage(page);
                }

                reader.Close();

                disk.UnLock();
            }
        }

        private BasePage ReadPageJournal(BinaryReader reader)
        {
            var stream = reader.BaseStream;
            var posStart = stream.Position * BasePage.PAGE_SIZE;
            var posEnd = posStart + BasePage.PAGE_SIZE;

            // Create page instance and read from disk (read page header + content page)
            var page = new BasePage();

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
                // read page content
                page.ReadContent(reader);
            }

            // read non-used bytes on page and position cursor to next page
            reader.ReadBytes((int)(posEnd - stream.Position));

            return page;
        }

        private void OpenExclusiveFile(string filename, Action<FileStream> success)
        {
            // check if is using by another process, if not, call fn passing stream opened
            try
            {
                using (var stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    success(stream);
                }
            }
            catch (IOException ex)
            {
                ex.WaitIfLocked(0);
            }
        }
    }
}
