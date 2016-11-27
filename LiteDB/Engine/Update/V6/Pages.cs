using System;
using System.Collections.Generic;

namespace LiteDB.Update.V6
{
    internal class BasePage_v6
    {
        public uint PageID { get; set; }
        public PageType PageType { get; set; }
        public uint PrevPageID { get; set; }
        public uint NextPageID { get; set; }
        public int ItemCount { get; set; }
    }

    internal class HeaderPage_v6 : BasePage_v6
    {
        public byte[] Password { get; set; }
        public Dictionary<string, uint> CollectionPages { get; set; }
    }

    internal class CollectionPage_v6 : BasePage_v6
    {
        public string CollectionName { get; set; }
        public long DocumentCount { get; set; }
        public PageAddress HeadNode { get; set; }
        public Dictionary<string, bool> Indexes { get; set; }
    }

    internal class IndexPage_v6 : BasePage_v6
    {
        public Dictionary<ushort, IndexNode_v6> Nodes { get; set; }
    }

    internal class DataPage_v6 : BasePage_v6
    {
        public Dictionary<ushort, DataBlock_v6> DataBlocks { get; set; }
    }

    internal class ExtendPage_v6 : BasePage_v6
    {
        public byte[] Data { get; set; }
    }

    internal class DataBlock_v6
    {
        public PageAddress Position { get; set; }
        public PageAddress[] IndexRef { get; set; }
        public uint ExtendPageID { get; set; }
        public byte[] Data { get; set; }
    }

    internal class IndexNode_v6
    {
        public PageAddress Position { get; set; }
        public PageAddress[] Prev { get; set; }
        public PageAddress[] Next { get; set; }
        public ushort KeyLength { get; set; }
        public BsonValue Key { get; set; }
        public PageAddress DataBlock { get; set; }
    }
}