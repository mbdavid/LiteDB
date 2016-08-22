using System;
using System.Collections;
using System.Collections.Generic;

namespace LiteDB
{
    public interface IDiskService : IDisposable
    {
        /// <summary>
        /// Open (and create if not exists) datafile
        /// </summary>
        void Open();

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
        /// Indicate if journal are enabled/implemented
        /// </summary>
        bool IsJournalEnabled { get; }

        /// <summary>
        /// Write original bytes page in a journal file (in sequence) - if journal not exists, create.
        /// </summary>
        void WriteJournal(uint pageID, byte[] page);

        /// <summary>
        /// Recovery journal file (if exists) - clear journal file after
        /// </summary>
        void Recovery();

        /// <summary>
        /// Clear jounal file
        /// </summary>
        void ClearJournal();
    }
}