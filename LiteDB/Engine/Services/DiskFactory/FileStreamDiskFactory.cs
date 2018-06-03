using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// FileStream disk implementation of disk factory
    /// </summary>
    public class FileStreamDiskFactory : IDiskFactory
    {
        private string _filename;
        private bool _readOnly;
        private bool _syncOverAsync;

        public FileStreamDiskFactory(string filename, bool readOnly, bool syncOverAsync)
        {
            _filename = filename;
            _readOnly = readOnly;
            _syncOverAsync = syncOverAsync;
        }

        /// <summary>
        /// Get filename
        /// </summary>
        public string Filename => _filename;

        /// <summary>
        /// Create new FileStream instance based on dataFilename
        /// </summary>
        public Stream GetStream()
        {
#if HAVE_SYNC_OVER_ASYNC
            if (_syncOverAsync)
            {
                return System.Threading.Tasks.Task.Run(() => GetStreamInternal())
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
#endif
            return GetStreamInternal();
        }

        private Stream GetStreamInternal()
        {
            return new FileStream(_filename,
                _readOnly ? FileMode.Open : FileMode.OpenOrCreate,
                _readOnly ? FileAccess.Read : FileAccess.ReadWrite,
                FileShare.ReadWrite,
                PAGE_SIZE,
                FileOptions.RandomAccess);
        }

        /// <summary>
        /// Close all stream on end
        /// </summary>
        public bool CloseOnDispose => true;
    }
}