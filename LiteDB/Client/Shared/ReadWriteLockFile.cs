using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;


namespace LiteDB
{
    internal class ReadWriteLockFile : IDisposable
    {
        private const int P_START = 0;
        private const int P_END = 1;
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

        public void AcquireLock(LockMode mode)
        {
            ENSURE(_slot == byte.MaxValue, "current process already locked");

            // get full lock file
            FileHelper.TryLock(_stream, _timeout);

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

                // and then unlock file
                FileHelper.TryUnlock(_stream);
            }
            else
            {
                // create item on list (waiting)
                this.SetRunning(_slot, mode, false);

                // write buffer into disk
                this.WriteBuffer();

                // unlock file and get mani looping to wait for my turn
                FileHelper.TryUnlock(_stream);

                var sw = Stopwatch.StartNew();

                while(sw.Elapsed < _timeout)
                {
                    Task.Delay(250).Wait();

                    var running = false;

                    // need lock file for this process (even if for read only)
                    FileHelper.TryLock(_stream, _timeout);

                    // update local array
                    this.ReadBuffer();

                    // check if is possible run now
                    if (this.CanRun(_slot, mode))
                    {
                        this.SetRunning(_slot, mode, true);

                        this.WriteBuffer();

                        running = true;
                    }

                    FileHelper.TryUnlock(_stream);

                    if (running) return;
                }

                // nesse ponto tenho que implementar o "rebuild" do arquivo, pois pode ser que o processo em execução
                // ou o processo que está aguardando execução (antes desse) tenha caido e o resto os processo não saibam
                // ou seja, preciso tentar "assumir" o controle com esse processo.... 
                // porem, pode ser um timeout "normal", ou seja, realmente tem uma thread que esta a 1 minuto com o processo
                // e não deve ocorrer "rebuild".
                throw new LiteException(0, "timeout");
            }
        }

        /// <summary>
        /// Release process lock (must run AcquireLock before)
        /// </summary>
        public void ReleaseLock()
        {
            ENSURE(_slot != byte.MaxValue, "must run AcquireLock before release lock");

            FileHelper.TryLock(_stream, _timeout);

            this.ReadBuffer();

            // clear current slot position
            _buffer[_slot + P_OFFSET] = 0;

            // get new start index
            var start = this.GetNewStartIndex();

            if (start != byte.MaxValue)
            {
                _buffer[P_START] = start;
            }

            // write buffer into disk
            this.WriteBuffer();

            // unlock file
            FileHelper.TryUnlock(_stream);

            _slot = byte.MaxValue;
        }

        private void Initialize()
        {
            _buffer[P_START] = byte.MaxValue;
            _buffer[P_END] = byte.MaxValue;

            // must lock?

            _stream.Position = 0;
            _stream.Write(_buffer, 0, FILE_SIZE);
        }

        /// <summary>
        /// Check on list current position to get new start index (checks for empty)
        /// </summary>
        private byte GetNewStartIndex()
        {
            var last = byte.MaxValue;
            var current = _buffer[P_START];

            while(_buffer[current + P_OFFSET] == 0)
            {
                current = (byte)((current + 1) % 255);

                last = current;

                if (current == _buffer[P_END]) break;
            }

            return last;
        }

        /// <summary>
        /// Return prev index based on current position. Returns byte.MaxValue if there is no more index
        /// </summary>
        private byte GetPrevIndex(byte current)
        {
            var prev = current - 1;

            if (prev < _buffer[P_START]) return byte.MaxValue;

            return (byte)((byte)prev % 255);
        }

        /// <summary>
        /// Checks if current slot can run now
        /// </summary>
        private bool CanRun(byte slot, LockMode mode)
        {
            // if slot is first position in list
            if (_buffer[P_START] == slot) return true;

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
        /// Get next free slot space in list. Throw exception if get more than 255
        /// </summary>
        private byte GetFreeSlot()
        {
            // first use
            if (_buffer[P_START] == byte.MaxValue)
            {
                _buffer[P_START] = _buffer[P_END] = 0;

                return 0;
            }

            // get next position
            var next = (byte)((_buffer[P_END] + 1) % 255);

            if (next == _buffer[P_START]) throw new LiteException(0, "There is more than 255 concurrent connection in datafile at shared mode");

            _buffer[P_END] = next;

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

        public string Debug
        {
            get
            {
                var sb = new StringBuilder();

                for(var i = 0; i < 255; i++)
                {
                    var p = _buffer[i + P_OFFSET];

                    var mode = p.GetBit(P_BIT_MODE) ? LockMode.Write : LockMode.Read;
                    var running = p.GetBit(P_BIT_RUNNING);

                    var c =
                        p == 0 ? '.' :
                        running ? (mode == LockMode.Read ? 'R' : 'W') :
                        (mode == LockMode.Read ? 'r' : 'w');

                    if (_buffer[P_START] == i) sb.Append("[");

                    sb.Append(c);

                    if (_buffer[P_END] == i) sb.Append("]");
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