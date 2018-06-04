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
        private string _dataFileName;
        private string _walFileName;
        private bool _readOnly;
        private bool _syncOverAsync;

        public FileStreamDiskFactory(string filename, bool readOnly, bool syncOverAsync)
        {
            _dataFileName = filename;
            _walFileName = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "-wal" + Path.GetExtension(filename));
            _readOnly = readOnly;
            _syncOverAsync = syncOverAsync;
        }

        /// <summary>
        /// Get data filename
        /// </summary>
        public string FileName => _dataFileName;

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetDataFileStream(bool write)
        {
#if HAVE_SYNC_OVER_ASYNC
            if (_syncOverAsync)
            {
                return System.Threading.Tasks.Task.Run(() => GetStreamInternal(_dataFileName, write, FileOptions.RandomAccess))
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
#endif
            return GetStreamInternal(_dataFileName, write, FileOptions.RandomAccess);
        }

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetWalFileStream(bool write)
        {
            var options = write ? FileOptions.SequentialScan : FileOptions.RandomAccess;

#if HAVE_SYNC_OVER_ASYNC
            if (_syncOverAsync)
            {
                return System.Threading.Tasks.Task.Run(() => GetStreamInternal(_dataFileName, write, options))
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
#endif
            return GetStreamInternal(_walFileName, write, options);
        }

        /// <summary>
        /// Open (or create) new FileStream based on filename. Can be sequencial (for WAL writer)
        /// Will be only 1 single writer, so I will open write mode with no more support for writer (will do file lock)
        /// </summary>
        private Stream GetStreamInternal(string filename, bool write, FileOptions options)
        {
            return new FileStream(filename,
                _readOnly || !write ? FileMode.Open : FileMode.OpenOrCreate,
                _readOnly || !write ? FileAccess.Read : FileAccess.ReadWrite,
                write ? FileShare.Read : FileShare.ReadWrite, // TODO: tenho duvia se nao precisa ser somente Write 
                PAGE_SIZE,
                options);
        }

        /// <summary>
        /// Check if wal file exists
        /// </summary>
        public bool IsWalFileExists()
        {
            return File.Exists(_walFileName);
        }

        /// <summary>
        /// Delete wal file
        /// </summary>
        public void DeleteWalFile()
        {
            File.Delete(_walFileName);
        }

        /// <summary>
        /// Close all stream on end
        /// </summary>
        public bool CloseOnDispose => true;
    }
}