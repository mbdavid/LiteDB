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

        public FileStreamDiskFactory(string filename)
        {
            _dataFilename = filename;
            _walFilename = FileHelper.GetTempFile(filename, "-wal", false);
            _tempFilename = new Lazy<string>(() => FileHelper.GetTempFile(filename, "-temp", true));
        }

        /// <summary>
        /// Get data filename
        /// </summary>
        public string Filename => _dataFilename;

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetDataFileStream(bool readOnly)
        {
            return this.GetStreamInternal(_dataFilename, FileOptions.RandomAccess, readOnly);
        }

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetWalFileStream(bool readOnly)
        {
            var options = readOnly ? FileOptions.RandomAccess : FileOptions.SequentialScan;

            return this.GetStreamInternal(_walFilename, options, readOnly);
        }

        /// <summary>
        /// Open (or create) new FileStream based on filename. Can be sequencial (for WAL writer)
        /// Will be only 1 single writer, so I will open write mode with no more support for writer (will do file lock)
        /// </summary>
        private Stream GetStreamInternal(string filename, FileOptions options, bool readOnly)
        {
            return new FileStream(filename,
                readOnly ? FileMode.Open : FileMode.OpenOrCreate,
                readOnly ? FileAccess.Read : FileAccess.ReadWrite,
                FileShare.ReadWrite,
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