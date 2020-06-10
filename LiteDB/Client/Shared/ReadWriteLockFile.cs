using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;


namespace LiteDB
{
    internal class ReadWriteLockFile : IDisposable
    {
        private const int P_POSITION = 0;
        private const int P_LENGTH = 1;
        private const int P_OFFSET = 2;
        private const int P_BIT_ITEM = 0;
        private const int P_BIT_RUNNING = 1;
        private const int P_BIT_MODE = 2;
        private const int FILE_SIZE = 1 + 1 + 255;

        private bool _disposedValue = false; // To detect redundant calls
        private readonly TimeSpan _timeout;
        private readonly string _lockFilename;
        private readonly FileStream _stream;

        private byte[] _buffer = new byte[FILE_SIZE];
        private byte _slot = byte.MaxValue;

        public ReadWriteLockFile(string lockFilename, TimeSpan timeout)
        {
            _timeout = timeout;
            _lockFilename = lockFilename;
            _stream = new FileStream(lockFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, FILE_SIZE);

            if (_stream.Length == 0)
            {
                this.Initialize();
            }
        }

        /// <summary>
        /// Get if current instance contains lock
        /// </summary>
        public bool IsLocked => _slot != byte.MaxValue;

        public void AcquireLock(LockMode mode, Action action)
        {
            ENSURE(_slot == byte.MaxValue, "current process already locked");

            // get full lock file
            FileHelper.TryLock(_stream, _timeout, true);

            // read all file into local array
            this.ReadBuffer();

            // get free slot position on lock file
            _slot = this.GetFreeSlot();

            // if it's possible to run now
            if (this.CanRun(_slot, mode))
            {
                // write file with running = true
                this.SetRunning(_slot, mode, true);

                // write buffer
                this.WriteBuffer();

                try
                {
                    // run action
                    action();
                }
                finally
                {
                    // and then unlock file
                    FileHelper.TryUnlock(_stream);
                }

                return;
            }
            else
            {
                // create item on queue
                this.SetRunning(_slot, mode, false);

                // write buffer into disk
                this.WriteBuffer();

                // unlock file and get main looping to wait for my turn
                FileHelper.TryUnlock(_stream);
            }

            var sw = Stopwatch.StartNew();

            // main loop (waiting for queue)
            while(sw.Elapsed < _timeout)
            {
                Task.Delay(250).Wait();

                var running = false;

                // need lock file for this process (even if for read only)
                FileHelper.TryLock(_stream, _timeout, true);

                // update local array
                this.ReadBuffer();

                // checks if my slot are emtpy (control was taken from first timeout instance)
                if (_buffer[_slot + P_OFFSET] == 0)
                {
                    // add me again in end of queue (get new slot)
                    _slot = this.GetFreeSlot();

                    this.SetRunning(_slot, mode, false);

                    this.WriteBuffer();
                }
                // check if is possible run now
                else if (this.CanRun(_slot, mode))
                {
                    this.SetRunning(_slot, mode, true);

                    this.WriteBuffer();

                    running = true;
                }

                // if current instance are running, 
                if (running)
                {
                    try
                    {
                        // run command
                        action();
                    }
                    finally
                    {
                        // close lock and exit
                        FileHelper.TryUnlock(_stream);
                    }

                    return;
                }

                FileHelper.TryUnlock(_stream);
            }

            // getting timeout here

            // try take control for this instance
            FileHelper.TryLock(_stream, _timeout, true);

            try
            {
                if (this.TryTakeControl(mode, action) == false)
                {
                    throw LiteException.LockTimeout("shared", _timeout);
                }
            }
            finally
            {
                FileHelper.TryUnlock(_stream);
            }
        }

        /// <summary>
        /// Release process lock (must run AcquireLock before)
        /// </summary>
        public void ReleaseLock()
        {
            ENSURE(_slot != byte.MaxValue, "must run AcquireLock before release lock");

            FileHelper.TryLock(_stream, _timeout, true);

            try
            {
                this.ReadBuffer();

                // clear current slot position
                _buffer[_slot + P_OFFSET] = 0;

                // update start (and length) of queue
                this.UpdateStartPosition();

                // write buffer into disk
                this.WriteBuffer();
            }
            finally
            {
                // unlock file
                FileHelper.TryUnlock(_stream);
            }

            _slot = byte.MaxValue;
        }

        /// <summary>
        /// Initialize lock file with full 0 and no start/end positions
        /// </summary>
        private void Initialize()
        {
            // write empty buffer
            _stream.Position = 0;
            _stream.Write(_buffer, 0, FILE_SIZE);
        }

        /// <summary>
        /// Update start/length on queue to remove unused slots
        /// </summary>
        private void UpdateStartPosition()
        {
            var length = _buffer[P_LENGTH];
            var current = _buffer[P_POSITION];

            // remove all first items that are empty (==0)
            while(_buffer[current + P_OFFSET] == 0)
            {
                current = (byte)((current + 1) % 255);

                length--;

                if (length == 0) break;
            }

            _buffer[P_POSITION] = current;
            _buffer[P_LENGTH] = length;
        }

        /// <summary>
        /// Return prev index based on passed position. Returns byte.MaxValue if are first of list
        /// </summary>
        private byte GetPrevIndex(byte current)
        {
            var prev = current - 1;

            if (prev < _buffer[P_POSITION]) return byte.MaxValue;

            return (byte)((byte)prev % 255);
        }

        /// <summary>
        /// Checks if current slot can run now
        /// </summary>
        private bool CanRun(byte slot, LockMode mode)
        {
            // if slot is first position in queue
            if (_buffer[P_POSITION] == slot) return true;

            if (mode == LockMode.Read)
            {
                var lindex = this.GetPrevIndex(slot);

                while(lindex != byte.MaxValue)
                {
                    // looking for last valid slot
                    var last = _buffer[lindex + P_OFFSET];
                    var lrunning = last.GetBit(P_BIT_RUNNING);
                    var lmode = last.GetBit(P_BIT_MODE) ? LockMode.Write : LockMode.Read;

                    // if slot are 0, it's empty slot, go to prev slot
                    if (last != 0)
                    {
                        // if prev slot are read-running, this slot can run too
                        if (lrunning && lmode == LockMode.Read)
                        {
                            return true;
                        }

                        return false;
                    }

                    lindex = this.GetPrevIndex(lindex);
                }
            }

            return false;
        }

        /// <summary>
        /// Set buffer slot value (mark as used, mode and running variables)
        /// </summary>
        private void SetRunning(byte slot, LockMode mode, bool running)
        {
            var value = ((byte)0)
                .SetBit(P_BIT_ITEM, true)
                .SetBit(P_BIT_RUNNING, running)
                .SetBit(P_BIT_MODE, mode == LockMode.Write);

            _buffer[_slot + P_OFFSET] = value;
        }

        /// <summary>
        /// Get next free slot space in queue. Throw exception if get more than 255
        /// </summary>
        private byte GetFreeSlot()
        {
            var length = _buffer[P_LENGTH] + 1;
            
            if (length >= 255) throw new LiteException(0, "There is more than 255 concurrent connection in datafile at shared mode");

            var next = (byte)((_buffer[P_POSITION] + length - 1) % 255);

            _buffer[P_LENGTH] = (byte)length;

            return next;
        }

        /// <summary>
        /// Read/update current buffer array with file data
        /// </summary>
        private void ReadBuffer()
        {
            _stream.Position = 0;
            _stream.Read(_buffer, 0, FILE_SIZE);
        }

        /// <summary>
        /// Write current buffer into disk
        /// </summary>
        private void WriteBuffer()
        {
            // update file
            _stream.Position = 0;
            _stream.Write(_buffer, 0, FILE_SIZE);
            _stream.FlushToDisk();
        }

        /// <summary>
        /// When throw timeout, try run action to take for this instance all control (and clean queue)
        /// </summary>
        private bool TryTakeControl(LockMode mode, Action action)
        {
            this.ReadBuffer();

            if (_buffer[_slot + P_OFFSET] == 0)
            {
                return false;
            }
            else
            {
                try
                {
                    // try execute action
                    action();

                    // add running instance in queue (at last position)
                    _slot = this.GetFreeSlot();

                    // clear all buffer
                    _buffer.Fill(0, 0, _buffer.Length);

                    // and crop queue 
                    _buffer[P_POSITION] = _slot;
                    _buffer[P_LENGTH] = 1;

                    // this this instance as running
                    this.SetRunning(_slot, mode, true);

                    this.WriteBuffer();

                    return true;
                }
                catch
                {
                    // clear buffer position
                    _buffer[_slot + P_OFFSET] = 0;

                    _slot = byte.MaxValue;

                    this.WriteBuffer();

                    return false;
                }
            }
        }

        public string Debug
        {
            get
            {
                var sb = new StringBuilder();
                var inside = false;
                var length = -1;

                for(var i = 0; i < 255; i++)
                {
                    var p = _buffer[i + P_OFFSET];

                    var mode = p.GetBit(P_BIT_MODE) ? LockMode.Write : LockMode.Read;
                    var running = p.GetBit(P_BIT_RUNNING);

                    var c =
                        p == 0 ? '.' :
                        running ? (mode == LockMode.Read ? 'R' : 'W') :
                        (mode == LockMode.Read ? 'r' : 'w');

                    if (_buffer[P_POSITION] == i)
                    {
                        sb.Append("[");
                        inside = true;
                        length++;
                    }

                    if (_buffer[P_LENGTH] == length)
                    {
                        sb.Append("]");
                        inside = false;
                        length = -1;
                    }

                    if (inside)
                    {
                        length++;
                    }

                    sb.Append(c);

                }

                return sb.ToString();
            }
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _stream.Dispose();

                FileHelper.TryDelete(_lockFilename);

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}