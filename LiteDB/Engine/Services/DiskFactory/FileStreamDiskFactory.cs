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
    internal class FileStreamDiskFactory : IDiskFactory
    {
        private readonly string _dataFilename;
        private readonly string _walFilename;
        private readonly Lazy<string> _tempFilename;
        private readonly bool _readonly;

        public FileStreamDiskFactory(string filename, bool readOnly)
        {
            _dataFilename = filename;
            _walFilename = FileHelper.GetTempFile(filename, "-wal", false);
            _tempFilename = new Lazy<string>(() => FileHelper.GetTempFile(filename, "-temp", true));

            _readonly = readOnly;
        }

        /// <summary>
        /// Get data filename
        /// </summary>
        public string Filename => _dataFilename;

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetDataFileStream()
        {
            return this.GetStreamInternal(_dataFilename, FileOptions.RandomAccess);
        }

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetWalFileStream(bool writeMode)
        {
            return this.GetStreamInternal(_walFilename, writeMode ? FileOptions.SequentialScan : FileOptions.RandomAccess);
        }

        /// <summary>
        /// Open (or create) new FileStream based on filename. Can be sequencial (for WAL writer)
        /// Will be only 1 single writer, so I will open write mode with no more support for writer (will do file lock)
        /// </summary>
        private Stream GetStreamInternal(string filename, FileOptions options)
        {
            return new FileStream(filename,
                _readonly ? FileMode.Open : FileMode.OpenOrCreate,
                _readonly ? FileAccess.Read : FileAccess.ReadWrite,
                _readonly ? FileShare.Read : FileShare.ReadWrite,
                PAGE_SIZE,
                options);
        }

        /// <summary>
        /// Check if wal file exists
        /// </summary>
        public bool IsWalFileExists()
        {
            return File.Exists(_walFilename);
        }

        /// <summary>
        /// Delete wal file
        /// </summary>
        public void DeleteWalFile()
        {
            File.Delete(_walFilename);
        }

        /// <summary>
        /// Close all stream on end
        /// </summary>
        public bool CloseOnDispose => true;
    }
}