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
        private string _dataFilename;
        private string _walFilename;

        public FileStreamDiskFactory(string filename)
        {
            _dataFilename = filename;
            _walFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "-wal" + Path.GetExtension(filename));
        }

        /// <summary>
        /// Get data filename
        /// </summary>
        public string Filename => _dataFilename;

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetDataFileStream(bool write)
        {
            return GetStreamInternal(_dataFilename, write, FileOptions.RandomAccess);
        }

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetWalFileStream(bool write)
        {
            var options = write ? FileOptions.SequentialScan : FileOptions.RandomAccess;

            return GetStreamInternal(_walFilename, write, options);
        }

        /// <summary>
        /// Open (or create) new FileStream based on filename. Can be sequencial (for WAL writer)
        /// Will be only 1 single writer, so I will open write mode with no more support for writer (will do file lock)
        /// </summary>
        private Stream GetStreamInternal(string filename, bool write, FileOptions options)
        {
            return new FileStream(filename,
                write == false ? FileMode.Open : FileMode.OpenOrCreate,
                write == false ? FileAccess.Read : FileAccess.ReadWrite,
                write ? FileShare.Read : FileShare.ReadWrite, // TODO: tenho duvia se nao precisa ser somente Write 
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