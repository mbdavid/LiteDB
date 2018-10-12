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
        private readonly string _logFilename;
        private readonly Lazy<string> _tempFilename;
        private readonly bool _readonly;

        public FileStreamDiskFactory(string filename, bool @readonly)
        {
            _dataFilename = filename;
            _logFilename = FileHelper.GetTempFile(filename, "-log", false);
            _tempFilename = new Lazy<string>(() => FileHelper.GetTempFile(filename, "-temp", true));
            _readonly = @readonly;
        }

        /// <summary>
        /// Get data filename
        /// </summary>
        public string Filename => _dataFilename;

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetDataFileStream(bool writeMode)
        {
            return this.GetInternalStream(_dataFilename, writeMode, FileOptions.SequentialScan);
        }

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetLogFileStream(bool writeMode)
        {
            return this.GetInternalStream(_logFilename, writeMode, FileOptions.SequentialScan);
        }

        private Stream GetInternalStream(string filename, bool writeMode, FileOptions options)
        {
            var write = writeMode && (_readonly == false);

            return new FileStream(filename,
                write ? FileMode.OpenOrCreate : FileMode.Open,
                write ? FileAccess.ReadWrite : FileAccess.Read,
                write ? FileShare.Read : FileShare.ReadWrite,
                PAGE_SIZE,
                options);
        }

        /// <summary>
        /// Check if wal file exists
        /// </summary>
        public bool IsLogFileExists()
        {
            return File.Exists(_logFilename);
        }

        /// <summary>
        /// Delete wal file
        /// </summary>
        public void DeleteLogFile()
        {
            File.Delete(_logFilename);
        }

        /// <summary>
        /// Close all stream on end
        /// </summary>
        public bool CloseOnDispose => true;
    }
}