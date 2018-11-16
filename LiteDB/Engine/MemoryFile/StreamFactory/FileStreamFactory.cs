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
    internal class FileStreamFactory : IStreamFactory
    {
        private readonly string _filename;
        private readonly DbFileMode _filemode;
        private readonly bool _readonly;

        public FileStreamFactory(string filename, DbFileMode filemode, bool readOnly)
        {
            _filename = filename;
            _filemode = filemode;
            _readonly = readOnly;
        }

        /// <summary>
        /// Get database file mode (data\log)
        /// </summary>
        public DbFileMode FileMode => _filemode;

        /// <summary>
        /// Get data filename
        /// </summary>
        public string Filename => _filename;

        /// <summary>
        /// Create new data file FileStream instance based on filename
        /// </summary>
        public Stream GetStream(bool canWrite, bool sequencial)
        {
            var write = canWrite && (_readonly == false);

            return new FileStream(_filename,
                _readonly ? System.IO.FileMode.Open : System.IO.FileMode.OpenOrCreate,
                write ? FileAccess.ReadWrite : FileAccess.Read,
                write ? FileShare.Read : FileShare.ReadWrite,
                PAGE_SIZE,
                sequencial ? FileOptions.SequentialScan : FileOptions.RandomAccess);
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
        /// Close all stream on end
        /// </summary>
        public bool CloseOnDispose => true;
    }
}