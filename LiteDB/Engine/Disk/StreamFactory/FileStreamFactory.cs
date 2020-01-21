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
    /// [ThreadSafe]
    /// </summary>
    internal class FileStreamFactory : IStreamFactory
    {
        private readonly string _filename;
        private readonly string _password;
        private readonly bool _readonly;

        public FileStreamFactory(string filename, string password, bool readOnly)
        {
            _filename = filename;
            _password = password;
            _readonly = readOnly;
        }

        /// <summary>
        /// Get data filename
        /// </summary>
        public string Name => Path.GetFileName(_filename);

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetStream(bool canWrite, bool sequencial)
        {
            var write = canWrite && (_readonly == false);

            var stream = new FileStream(_filename,
                _readonly ? System.IO.FileMode.Open : System.IO.FileMode.OpenOrCreate,
                write ? FileAccess.ReadWrite : FileAccess.Read,
                write ? FileShare.Read : FileShare.ReadWrite,
                PAGE_SIZE,
                sequencial ? FileOptions.SequentialScan : FileOptions.RandomAccess);

            return _password == null ? (Stream)stream : new AesStream(_password, stream);
        }

        /// <summary>
        /// Get file length using FileInfo
        /// </summary>
        public long GetLength()
        {
            // getting size from OS - if encrypted must remove salt first page
            return new FileInfo(_filename).Length - (_password == null ? 0 : PAGE_SIZE);
        }

        /// <summary>
        /// Check if file exists (without open it)
        /// </summary>
        public bool Exists()
        {
            return File.Exists(_filename);
        }

        /// <summary>
        /// Delete file (must all stream be closed)
        /// </summary>
        public void Delete()
        {
            File.Delete(_filename);
        }

        /// <summary>
        /// Test if this file are locked by another process
        /// </summary>
        public bool IsLocked() => this.Exists() && FileHelper.IsFileLocked(_filename);

        /// <summary>
        /// Close all stream on end
        /// </summary>
        public bool CloseOnDispose => true;
    }
}