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

        public SysDump(HeaderPage header, TransactionMonitor monitor) : base("$dump")
        {
            _header = header;
            _monitor = monitor;
        }

        public override IEnumerable<BsonDocument> Input(BsonValue options)
        {
            var pageID = GetOption(options, "pageID");

            return this.DumpPages(pageID == null ? null : (uint?)pageID.AsInt32);
        }

        private IEnumerable<BsonDocument> DumpPages(uint? pageID)
        {
            var collections = _header.GetCollections().ToDictionary(x => x.Value, x => x.Key);

            // get any transaction from current thread ID
            var transaction = _monitor.GetThreadTransaction();

            var snapshot = transaction.CreateSnapshot(LockMode.Read, "$", false);

            var start = pageID.HasValue ? pageID.Value : 0;
            var end = pageID.HasValue ? pageID.Value : _header.LastPageID;

            for (uint i = start; i <= Math.Min(end, _header.LastPageID); i++)
            {
                var page = snapshot.GetPage<BasePage>(i, out var origin, out var position, out var walVersion);

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
                    ["highestIndex"] = (int)page.HighestIndex
                };

                if (pageID.HasValue) doc["buffer"] = page.Buffer.ToArray();

                yield return doc;

                transaction.Safepoint();
            }
        }
    }
}