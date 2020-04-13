using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class SysDump : SystemCollection
    {
        private readonly HeaderPage _header;
        private readonly TransactionMonitor _monitor;
        private readonly DiskService _disk;

        public SysDump(HeaderPage header, TransactionMonitor monitor, DiskService disk) : base("$dump")
        {
            _header = header;
            _monitor = monitor;
            _disk = disk;
        }

        private BsonValue Number(long num) => num < int.MaxValue ? new BsonValue((int)num) : new BsonValue(num);

        public override IEnumerable<BsonDocument> Input(BsonValue options)
        {
            var pageID = GetOption(options, "pageID");

            if (pageID == null)
            {
                return DumpPages();
            }
            else
            {
                return DumpPages((uint)pageID.AsInt32);
            }
        }

        /// <summary>
        /// Get all pages from datafile based only in Page Position (get from disk - not from cache)
        /// </summary>
        private IEnumerable<BsonDocument> DumpPages()
        {
            var collections = _header.GetCollections().ToDictionary(x => x.Value, x => x.Key);

            foreach (var buffer in _disk.ReadFull())
            {
                var page = new BasePage(buffer);

                var doc = new BsonDocument
                {
                    ["position"] = Number(buffer.Position),
                    ["origin"] = page.PageID == 0 && page.TransactionID == 0 && page.PageType == PageType.Empty ? "blank" :
                        buffer.Position < _disk.LogStartPosition ? "data" : "log",

                    ["pageID"] = (int)page.PageID,
                    ["pageType"] = page.PageType.ToString(),
                    ["nextPageID"] = (int)page.NextPageID,
                    ["prevPageID"] = (int)page.PrevPageID,
                    ["collection"] = collections.GetOrDefault(page.ColID, "-"),

                    ["transactionID"] = (int)page.TransactionID,
                    ["isConfirmed"] = page.IsConfirmed,

                    ["itemsCount"] = (int)page.ItemsCount,
                    ["freeBytes"] = page.FreeBytes,
                    ["usedBytes"] = (int)page.UsedBytes,
                    ["fragmentedBytes"] = (int)page.FragmentedBytes,
                    ["nextFreePosition"] = (int)page.NextFreePosition,
                    ["highestIndex"] = (int)page.HighestIndex
                };

                yield return doc;
            }
        }

        /// <summary>
        /// Get a single page from database based on newsest version of page (use only PageID)
        /// </summary>
        private IEnumerable<BsonDocument> DumpPages(uint pageID)
        {
            var collections = _header.GetCollections().ToDictionary(x => x.Value, x => x.Key);

            // get any transaction from current thread ID
            var transaction = _monitor.GetThreadTransaction();

            var snapshot = transaction.CreateSnapshot(LockMode.Read, "$", false);

            var page = pageID == 0 ? _header : snapshot.GetPage<BasePage>(pageID);

            var doc = new BsonDocument
            {
                ["pageID"] = (int)page.PageID,
                ["pageType"] = page.PageType.ToString(),
                ["nextPageID"] = (int)page.NextPageID,
                ["prevPageID"] = (int)page.PrevPageID,
                ["collection"] = collections.GetOrDefault(page.ColID, "-"),
                ["itemsCount"] = (int)page.ItemsCount,
                ["freeBytes"] = page.FreeBytes,
                ["usedBytes"] = (int)page.UsedBytes,
                ["fragmentedBytes"] = (int)page.FragmentedBytes,
                ["nextFreePosition"] = (int)page.NextFreePosition,
                ["highestIndex"] = (int)page.HighestIndex,
                ["buffer"] = page.Buffer.ToArray()
            };

            yield return doc;
        }
    }
}