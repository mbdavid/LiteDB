using System;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// A simple implementation of diskservice using base Stream (no journal, thread safe)
    /// </summary>
    internal class StreamDiskService : IDiskService
    {
        private Stream _stream;

        public StreamDiskService(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Checks only if stream is empty
        /// </summary>
        public bool Initialize()
        {
            return (_stream.Length == 0);
        }

        /// <summary>
        /// Create new database - just create empty header page
        /// </summary>
        public void CreateNew()
        {
            this.WritePage(0, new HeaderPage().WritePage());
        }

        #region Lock/Unlock

        /// <summary>
        /// Lock stream
        /// </summary>
        public void Lock()
        {
        }

        /// <summary>
        /// Release lock
        /// </summary>
        public void Unlock()
        {
        }

        #endregion Lock/Unlock

        #region Read/Write

        /// <summary>
        /// Read first 2 bytes from datafile - contains changeID (avoid to read all header page)
        /// </summary>
        public ushort GetChangeID()
        {
            var bytes = new byte[2];
            _stream.Seek(HeaderPage.CHANGE_ID_POSITION, SeekOrigin.Begin);
            _stream.Read(bytes, 0, 2);
            return BitConverter.ToUInt16(bytes, 0);
        }

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public byte[] ReadPage(uint pageID)
        {
            var buffer = new byte[BasePage.PAGE_SIZE];
            var position = BasePage.GetSizeOfPages(pageID);

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            // read bytes from data file
            _stream.Read(buffer, 0, BasePage.PAGE_SIZE);

            return buffer;
        }

        /// <summary>
        /// Persist single page bytes to disk
        /// </summary>
        public void WritePage(uint pageID, byte[] buffer)
        {
            var position = BasePage.GetSizeOfPages(pageID);

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            _stream.Write(buffer, 0, BasePage.PAGE_SIZE);
        }

        /// <summary>
        /// Set datafile length (not used)
        /// </summary>
        public void SetLength(long fileSize)
        {
        }

        #endregion Read/Write

        #region Journal file

        public void WriteJournal(uint pageID, byte[] data)
        {
        }

        public void DeleteJournal()
        {
        }

        public void Dispose()
        {
            // keep _stream open for external use
        }

        #endregion Journal file

        #region Temporary

        public IDiskService GetTempDisk()
        {
            return new StreamDiskService(new MemoryStream());
        }

        public void DeleteTempDisk()
        {
            // nothing to do
        }

        #endregion Temporary
    }
}