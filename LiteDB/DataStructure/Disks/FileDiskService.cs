using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LiteDB
{
    internal class FileDiskService : IDiskService
    {
        private const int LOCK_POSITION = 0;

        private FileStream _stream;
        private string _filename;

        private byte[] _buffer = new byte[BasePage.PAGE_SIZE];

        public FileDiskService(string filename)
        {
            _filename = filename;
        }
        
        /// <summary>
        /// Open datafile - if not exits, create a new one
        /// </summary>
        public void Initialize()
        {
            // open file as readOnly - if we need use Write, re-open in Write Mode
            _stream = new FileStream(_filename, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite, BasePage.PAGE_SIZE);

            if (_stream.Length == 0)
            {
                this.WritePage(0, new HeaderPage().WritePage());
            }
        }

        /// <summary>
        /// Lock datafile agains other process read/write
        /// </summary>
        public void Lock()
        {
            TryExec(() => _stream.Lock(LOCK_POSITION, 1));
        }

        /// <summary>
        /// Release lock
        /// </summary>
        public void Unlock()
        {
            _stream.Unlock(LOCK_POSITION, 1);
        }

        /// <summary>
        /// Read page bytes from disk
        /// </summary>
        public byte[] ReadPage(uint pageID)
        {
            var position = (long)pageID * (long)BasePage.PAGE_SIZE;

            TryExec(() => 
            {
                // position cursor
                if (_stream.Position != position)
                {
                    _stream.Seek(position, SeekOrigin.Begin);
                }

                // read bytes from data file
                _stream.Read(_buffer, 0, BasePage.PAGE_SIZE); 

            });

            return _buffer;
        }

        /// <summary>
        /// Persist single page bytes to disk
        /// </summary>
        public void WritePage(uint pageID, byte[] buffer)
        {
            if(_stream.CanWrite == false)
            {
                _stream.Dispose();
                _stream = new FileStream(_filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, BasePage.PAGE_SIZE);
            }

            var position = (long)pageID * (long)BasePage.PAGE_SIZE;

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            _stream.Write(buffer, 0, BasePage.PAGE_SIZE);
        }

        public void Dispose()
        {
            if(_stream != null)
            {
                _stream.Dispose();
            }
        }

        /// <summary>
        /// Try run an operation over datafile - keep tring if locked
        /// </summary>
        private static void TryExec(Action action)
        {
            var timeout = new TimeSpan(0, 1, 0);
            var timer = DateTime.Now.Add(timeout);

            while (DateTime.Now < timer)
            {
                try
                {
                    action();
                    return;
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(250);
                }
                catch (IOException ex)
                {
                    ex.WaitIfLocked(250);
                }
            }

            throw LiteException.LockTimeout(timeout);
        }
    }
}
