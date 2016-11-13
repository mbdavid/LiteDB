using System;
using System.Collections;
using System.Collections.Generic;

namespace LiteDB
{
    public interface IDiskService : IDisposable
    {
        /// <summary>
        /// Open data file (creating if doest exists) and validate header
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
        /// Indicate if journal are enabled/implemented
        /// </summary>
        bool IsJournalEnabled { get; }

        /// <summary>
        /// Read journal file returning IEnumerable of pages
        /// </summary>
        IEnumerable<byte[]> ReadJournal();

        /// <summary>
        /// Write original bytes page in a journal file (in sequence) - if journal not exists, create.
        /// </summary>
        void WriteJournal(uint pageID, byte[] page);

        /// <summary>
        /// Clear jounal file
        /// </summary>
        void ClearJournal();
    }
}