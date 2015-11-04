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
        /// <summary>
        /// Lock data file position
        /// </summary>
        private const int LOCK_POSITION = 0;

        private FileStream _stream;
        private string _filename;
        private TimeSpan _timeout;
        private bool _journal;
        //private bool _readonly;

        private byte[] _buffer = new byte[BasePage.PAGE_SIZE];

        public FileDiskService(string filename, bool journal, TimeSpan timeout)
        {
            _filename = filename;
            _journal = journal;
            _timeout = timeout;
        }
        
        /// <summary>
        /// Open datafile - returns true if new
        /// </summary>
        public bool Initialize()
        {
            // open data file
            _stream = new FileStream(_filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, BasePage.PAGE_SIZE);

            // returns true if new file
            return _stream.Length == 0;
        }

        /// <summary>
        /// Lock datafile agains other process read/write
        /// </summary>
        public void Lock()
        {
            TryExec(() => 
                _stream.Lock(LOCK_POSITION, 1)
            );
        }

        /// <summary>
        /// Release lock
        /// </summary>
        public void Unlock()
        {
            _stream.Unlock(LOCK_POSITION, 1);
        }

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
            var position = (long)pageID * (long)BasePage.PAGE_SIZE;

            // position cursor
            if (_stream.Position != position)
            {
                _stream.Seek(position, SeekOrigin.Begin);
            }

            _stream.Write(buffer, 0, BasePage.PAGE_SIZE);
        }

        private void TryRecovery()
        {
            // se tiver journal, faz recovery
        }

        public void ChangePage(uint pageID, byte[] original)
        {
            // grava o journal-file
        }

        public void StartWrite()
        {
            // comita o journal file
        }

        public void EndWrite()
        {
            // remove journal file
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
        private void TryExec(Action action)
        {
            var timer = DateTime.Now.Add(_timeout);

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

            throw LiteException.LockTimeout(_timeout);
        }
    }
}
