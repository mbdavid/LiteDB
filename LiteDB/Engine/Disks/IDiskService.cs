using System;
using System.Collections;
using System.Collections.Generic;

namespace LiteDB
{
    public interface IDiskService : IDisposable
    {
        /// <summary>
        /// Open data file (creating if doest exists) and return header content bytes
        /// </summary>
        void Initialize(Logger log, string password);

        /// <summary>
        /// Read a page from disk datafile
        /// </summary>
        byte[] ReadPage(uint pageID);

        /// <summary>
        /// Write a page in disk datafile
        /// </summary>
        void WritePage(uint pageID, byte[] buffer);

        /// <summary>
        /// Set datafile length before start writing in disk
        /// </summary>
        void SetLength(long fileSize);

        /// <summary>
        /// Gets file length in bytes
        /// </summary>
        long FileLength { get; }

        /// <summary>
        /// Ensures all pages from the OS cache are persisted on medium
        /// </summary>
        void Flush();
    }
}