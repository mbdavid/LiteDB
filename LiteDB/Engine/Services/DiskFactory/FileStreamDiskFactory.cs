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

        public FileStreamDiskFactory(string filename, bool @readonly)
        {
            _dataFilename = filename;
            _walFilename = FileHelper.GetTempFile(filename, "-wal", false);
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
        public Stream GetWalFileStream(bool writeMode)
        {
            return this.GetInternalStream(_walFilename, writeMode, FileOptions.SequentialScan);
        }

        private Stream GetInternalStream(string filename, bool writeMode, FileOptions options)
        {
            //TODO: atualmente não posso abrir o arquivo como somente leitura pq o BinaryWriter obriga que o Stream suporte escrita
            // preciso alterar a regra do negocio

            var write = writeMode; // && _readonly == false;

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