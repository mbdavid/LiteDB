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

        #endregion

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
            var position = (long)pageID * (long)BasePage.PAGE_SIZE;

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
            var position = (long)pageID * (long)BasePage.PAGE_SIZE;

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            _stream.Write(buffer, 0, BasePage.PAGE_SIZE);
        }

        #endregion

        #region Journal file

        public void WriteJournal(uint pageID, byte[] data)
        {
        }

        public void CommitJournal(long fileSize)
        {
        }

        public void DeleteJournal()
        {
        }

        public void Dispose()
        {
            if(_stream != null)
            {
                _stream.Dispose();
            }
        }

        #endregion

        #region Temporary

        public IDiskService GetTempDisk()
        {
            return new StreamDiskService(new MemoryStream());
        }

        public void DeleteTempDisk()
        {
            // nothing to do
        }

        #endregion
    }
}
