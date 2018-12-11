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
            return 0;

            /*
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
                            var buffer = reader.ReadPage(position, true, FileOrigin.Log);
                            var page = new BasePage(buffer);

                            if (sortedConfirmTransactions.Contains(page.TransactionID))
                            {
                                buffer.Position = BasePage.GetPagePosition(page.PageID);

                                yield return buffer;
                            }
                            else
                            {
                                _disk.DiscardPages(new[] { buffer }, false);
                            }

                            position += PAGE_SIZE;

                            buffer.Release();
                        }

                        // update header page with last checkpoint
                        // _header.LastCheckpoint = DateTime.UtcNow;

                        var headerBuffer = _header.GetBuffer(true);
                        var clone = reader.NewPage();
                        clone.Position = 0;

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
            */
        }
    }
}