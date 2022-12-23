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
        private readonly bool _hidden;

        public FileStreamFactory(string filename, string password, bool readOnly, bool hidden)
        {
            _filename = filename;
            _password = password;
            _readonly = readOnly;
            _hidden = hidden;
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

            var isNewFile = write && this.Exists() == false;

            var stream = new FileStream(_filename,
                _readonly ? System.IO.FileMode.Open : System.IO.FileMode.OpenOrCreate,
                write ? FileAccess.ReadWrite : FileAccess.Read,
                write ? FileShare.Read : FileShare.ReadWrite,
                PAGE_SIZE,
                sequencial ? FileOptions.SequentialScan : FileOptions.RandomAccess);

            if (isNewFile && _hidden)
            {
                File.SetAttributes(_filename, FileAttributes.Hidden);
            }

            return _password == null ? (Stream)stream : new AesStream(_password, stream);
        }

        /// <summary>
        /// Get file length using FileInfo
        /// </summary>
        public long GetLength()
        {
            // if not file do not exists, returns 0
            if (!this.Exists()) return 0;

            // get physical file length from OS
            var length = new FileInfo(_filename).Length;

            // if length < PAGE_SIZE, ignore file length (should be 0)
            if (length < PAGE_SIZE) return 0;

            ENSURE(length % PAGE_SIZE == 0, $"file length must be PAGE_SIZE module. length={length}, file={Path.GetFileName(_filename)}");

            // if encrypted must remove salt first page
            return length - (_password == null ? 0 : PAGE_SIZE);
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