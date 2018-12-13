using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Do many database checks to verify if data file are consistent
        /// </summary>
        public StringBuilder CheckIntegrity()
        {
            _locker.EnterReserved(true);

            _disk.Queue.Wait();

            var report = new StringBuilder();

            report.AppendLine("LiteDB Check Integrity Report");
            report.AppendLine("=============================");

            Run("Filename", "{0}", () => _disk.GetName(FileOrigin.Data));
            Run("Verity CRC data file", "OK ({0} pages)", () => this.VerifyPageCRC(FileOrigin.Data));
            Run("Verity CRC log file", "OK ({0} pages)", () => this.VerifyPageCRC(FileOrigin.Log));
            Run("Verity free empty list", "OK ({0} pages)", () => this.VerifyFreeEmptyList());
            Run("Verity data pages links", "OK ({0} pages)", () => this.VerifyPagesType(PageType.Data));
            Run("Verity index pages links", "OK ({0} pages)", () => this.VerifyPagesType(PageType.Index));

            void Run(string title, string ok, Func<object> action)
            {
                report.Append(title.PadRight(25, '.') + ": ");

                try
                {
                    var result = action();
                    report.AppendLine(string.Format(ok, result));
                }
                catch (Exception ex)
                {
                    report.AppendLine("ERR: " + ex.Message);
                }
            };

            _locker.ExitReserved(true);

            return report;
        }

        /// <summary>
        /// Checks file CRC
        /// </summary>
        private int VerifyPageCRC(FileOrigin origin)
        {
            var counter = 0;

            foreach (var buffer in _disk.ReadFull(origin))
            {
                // do not check CRC at Page 1 when encryption datafile (salt page)
                if (buffer.Position == 1 && _settings.Password != null && origin == FileOrigin.Data) continue;

                var page = new BasePage(buffer);

                if (page.CRC != page.ComputeChecksum())
                {
                    throw new LiteException(0, $"Invalid CRC at page {page.PageID}");
                }

                counter++;
            }

            return counter;
        }

        /// <summary>
        /// Check if all free empty list pages are OK
        /// </summary>
        private int VerifyFreeEmptyList()
        {
            return this.AutoTransaction(transaction =>
            {
                var snapshot = transaction.CreateSnapshot(LockMode.Read, "_", false);
                var next = _header.FreeEmptyPageID;
                var counter = 0;

                while(next != uint.MaxValue)
                {
                    var page = snapshot.GetPage<BasePage>(next);

                    if (page.PageType != PageType.Empty)
                    {
                        throw new LiteException(0, $"Page {page.PageID} should be Empty type");
                    }

                    transaction.Safepoint();

                    counter++;
                }

                return counter;
            });
        }

        /// <summary>
        /// Checks if all data/index pages are linked
        /// </summary>
        private int VerifyPagesType(PageType type)
        {
            return this.AutoTransaction(transaction =>
            {
                var counter = 0;

                foreach (var col in _header.GetCollections())
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Read, col.Key, false);

                    for (var slot = 0; slot < 5; slot++)
                    {
                        var next = type == PageType.Data ?
                            snapshot.CollectionPage.FreeDataPageID[slot] :
                            snapshot.CollectionPage.FreeIndexPageID[slot];

                        while (next != uint.MaxValue)
                        {
                            var page = snapshot.GetPage<BasePage>(next);

                            if (page.PageType != type)
                            {
                                throw new LiteException(0, $"Page {page.PageID} should be {type} type");
                            }

                            counter++;
                            next = page.NextPageID;
                            transaction.Safepoint();
                        }
                    }
                }

                return counter;
            });
        }
    }
}