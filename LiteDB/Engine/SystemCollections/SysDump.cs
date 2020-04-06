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
        /// Get all pages from datafile based only in Page Position
        /// </summary>
        /// <returns></returns>
        private IEnumerable<BsonDocument> DumpPages()
        {
            var collections = _header.GetCollections().ToDictionary(x => x.Value, x => x.Key);

            foreach (var buffer in _disk.ReadFull())
            {
                var page = new BasePage(buffer);

                var doc = new BsonDocument
                {
                    ["pageID"] = (int)page.PageID,
                    ["pageType"] = page.PageType.ToString(),
                    ["_position"] = buffer.Position,
                    ["_origin"] = buffer.Position < _disk.LogStartPosition ? FileOrigin.Data.ToString() : FileOrigin.Log.ToString(),
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

            var page = snapshot.GetPage<BasePage>(pageID, out var origin, out var position, out var walVersion);

            var doc = new BsonDocument
            {
                ["pageID"] = (int)page.PageID,
                ["pageType"] = page.PageType.ToString(),
                ["_position"] = position,
                ["_origin"] = origin.ToString(),
                ["_version"] = walVersion,
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