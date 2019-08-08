using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// </summary>
        public long Shrink()
        {
            _walIndex.Checkpoint(false);

            if (_disk.GetLength(FileOrigin.Log) > 0) throw new LiteException(0, "Shrink operation requires no log file - run Checkpoint before continue");

            _locker.EnterReserved(true);

            var originalLength = _disk.GetLength(FileOrigin.Data);

            // create a savepoint in header page - restore if any error occurs
            var savepoint = _header.Savepoint();

            // must clear all cache pages because all of them will change
            _disk.Cache.Clear();

            try
            {
                // initialize V8 file reader
                using (var reader = new FileReaderV8(_header, _disk))
                {
                    // clear current header
                    _header.FreeEmptyPageID = uint.MaxValue;
                    _header.LastPageID = 0;
                    _header.GetCollections().ToList().ForEach(c => _header.DeleteCollection(c.Key));

                    // rebuild entrie database using FileReader
                    this.Rebuild(reader);

                    // crop data file
                    var newLength = BasePage.GetPagePosition(_header.LastPageID);

                    _disk.SetLength(newLength, FileOrigin.Data);

                    return originalLength - newLength;
                }
            }
            catch(Exception)
            {
                _header.Restore(savepoint);

                throw;
            }
            finally
            {
                _locker.ExitReserved(true);

                _walIndex.Checkpoint(false);
            }
        }
    }
}