using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    /// <summary>
    /// FileStream disk implementation of disk factory
    /// </summary>
    public class FileStreamDiskFactory : IDiskFactory
    {
        private string _dataFilename;
        private string _walFilename;
        private bool _readOnly;

        public FileStreamDiskFactory(string filename, bool readOnly)
        {
            _dataFilename = filename;
            _walFilename = FileHelper.GetTempFile(filename, "-wal", false);
            _readOnly = readOnly;
        }

        /// <summary>
        /// Limit in 100 concurrency file stream opened (fixed for now)
        /// </summary>
        public int ConcurrencyLimit => 100;

        /// <summary>
        /// Close all stream on end
        /// </summary>
        public bool Dispose => true;

        /// <summary>
        /// Create new FileStream instance based on dataFilename
        /// </summary>
        public Stream GetDataFile()
        {
            return new FileStream(_dataFilename,
                _readOnly ? FileMode.Open : FileMode.OpenOrCreate,
                _readOnly ? FileAccess.Read : FileAccess.ReadWrite,
                FileShare.ReadWrite,
                BasePage.PAGE_SIZE,
                FileOptions.RandomAccess);
        }

        /// <summary>
        /// Create new FileStream instance based on walFilename
        /// </summary>
        public Stream GetWalFile()
        {
            if (_readOnly) throw LiteException.ReadOnlyDatabase();

            return new FileStream(_dataFilename,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                BasePage.PAGE_SIZE,
                FileOptions.RandomAccess);
        }
    }
}