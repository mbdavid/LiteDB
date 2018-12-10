using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Do a full WAL checkpoint coping old log file into data file.
        /// </summary>
        public int Checkpoint()
        {
            _locker.EnterReserved(true);

            try
            {
                // get sorted confirmed transaction to check
                var sortedConfirmTransactions = new HashSet<long>(_walIndex.ConfirmedTransactions);

                IEnumerable<PageBuffer> source()
                {
                    var position = 0L;
                    var length = _disk.GetLength(FileOrigin.Log);

                    using (var reader = _disk.GetReader())
                    {
                        while(position < length)
                        {
                            var buffer = reader.ReadPage(position, false, FileOrigin.Log);
                            var page = new BasePage(buffer);

                            if (sortedConfirmTransactions.Contains(page.TransactionID))
                            {
                                yield return buffer;
                            }

                            position += PAGE_SIZE;

                            buffer.Release();
                        }

                        // update header page with last checkpoint
                        _header.LastCheckpoint = DateTime.Now;

                        var headerBuffer = _header.GetBuffer(true);
                        var clone = reader.NewPage();

                        Buffer.BlockCopy(headerBuffer.Array, headerBuffer.Offset, clone.Array, clone.Offset, clone.Count);

                        yield return clone;

                        clone.Release();
                    }
                }

                _disk.Write(source(), FileOrigin.Data);

                // clear wal-index
                _walIndex.Clear();

                // shrink log file
                _disk.SetLength(0, FileOrigin.Log);
            }
            finally
            {
                _locker.ExitReserved(true);
            }

            return 0;
        }
    }
}