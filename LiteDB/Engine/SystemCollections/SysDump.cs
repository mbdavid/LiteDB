using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        private IEnumerable<BsonDocument> SysDump(FileOrigin origin)
        {
            var collections = _header.GetCollections().ToDictionary(x => x.Value, x => x.Key);

            foreach(var buffer in _disk.ReadFull(origin))
            {
                var page = new BasePage(buffer);

                var doc = new BsonDocument();

                doc["_position"] = (int)buffer.Position;
                doc["_version"] = 0;

                doc["pageID"] = (int)page.PageID;
                doc["pageType"] = page.PageType.ToString();
                doc["nextPageID"] = dumpPageID(page.NextPageID);
                doc["prevPageID"] = dumpPageID(page.PrevPageID);
                doc["crc"] = (int)page.CRC;

                doc["collection"] = collections.GetOrDefault(page.ColID, "-");
                doc["transactionID"] = (int)page.TransactionID;
                doc["isConfirmed"] = page.IsConfirmed;

                doc["itemsCount"] = (int)page.ItemsCount;
                doc["usedContentBlocks"] = (int)page.UsedContentBlocks;
                doc["fragmentedBlocks"] = (int)page.FragmentedBlocks;
                doc["nextFreeBlock"] = (int)page.NextFreeBlock;
                doc["highestIndex"] = (int)page.HighestIndex;

                yield return doc;
            }

            BsonValue dumpPageID(uint pageID)
            {
                return pageID == uint.MaxValue ? BsonValue.Null : new BsonValue((int)pageID);
            }
        }
    }
}