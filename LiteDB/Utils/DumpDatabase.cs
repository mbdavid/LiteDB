using System;
using System.Collections.Generic;
using LiteDB;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// A debugger class to show how pages are storaged. Used to debug pages in shell/tests
    /// </summary>
    internal class DumpDatabase
    {
        public static StringBuilder Pages(LiteDatabase db, bool mem)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Dump - " + (mem ? "Cache/Disk" : "Only Disk"));

            for (uint i = 0; i <= db.Cache.Header.LastPageID; i++)
            {
                var p = ReadPage<BasePage>(db, i, mem);

                sb.AppendFormat("{0} <{1},{2}> [{3}] {4}{5} | ",
                    p.PageID.Dump(),
                    p.PrevPageID.Dump(),
                    p.NextPageID.Dump(),
                    p.PageType.ToString().PadRight(6).Substring(0, 6),
                    p.FreeBytes.ToString("0000"),
                    p.IsDirty ? "d" : " ");

                if (p.PageType == PageType.Header) p = ReadPage<HeaderPage>(db, i, mem);
                if (p.PageType == PageType.Collection) p = ReadPage<CollectionPage>(db, i, mem);
                if (p.PageType == PageType.Data) p = ReadPage<DataPage>(db, i, mem);
                if (p.PageType == PageType.Extend) p = ReadPage<ExtendPage>(db, i, mem);
                if (p.PageType == PageType.Index) p = ReadPage<IndexPage>(db, i, mem);

                p.Dump(sb);
                sb.AppendLine();
            }

            return sb;
        }

        private static T ReadPage<T>(LiteDatabase db, uint pageID, bool mem)
            where T : BasePage, new()
        {
            if (mem && pageID == 0) return (T)(object)db.Cache.Header;

            return mem ? db.Pager.GetPage<T>(pageID) : db.Disk.ReadPage<T>(pageID);
        }

        public static StringBuilder Index(LiteDatabase db, string collection, string field, int size = 5)
        {
            var sbs = new StringBuilder[IndexNode.MAX_LEVEL_LENGTH + 1];

            var col = db.GetCollection(collection).GetCollectionPage(false);
            if (col == null) throw new ArgumentException("Invalid collection name");

            var index = col.GetIndex(field);
            if (index == null) throw new ArgumentException("Invalid index field name");

            for (var i = 0; i < sbs.Length; i++)
            {
                sbs[i] = new StringBuilder();
            }

            var cur = index.HeadNode;

            while (!cur.IsEmpty)
            {
                var page = db.Pager.GetPage<IndexPage>(cur.PageID);
                var node = page.Nodes[cur.Index];

                sbs[0].Append((Limit(node.Key.ToString(), size)).PadBoth(1 + (2 * size)));

                for (var i = 0; i < IndexNode.MAX_LEVEL_LENGTH; i++)
                {
                    var sb = sbs[i + 1];
                    var p = " ";
                    var n = " ";

                    if (i < node.Prev.Length)
                    {
                        if (!node.Prev[i].IsEmpty)
                        {
                            var pprev = db.Pager.GetPage<IndexPage>(node.Prev[i].PageID);
                            var pnode = pprev.Nodes[node.Prev[i].Index];
                            p = pnode.Key.ToString();
                        }
                        if (!node.Next[i].IsEmpty)
                        {
                            var pnext = db.Pager.GetPage<IndexPage>(node.Next[i].PageID);
                            var pnode = pnext.Nodes[node.Next[i].Index];
                            n = pnode.Key.ToString();
                        }
                    }

                    sb.Append(Limit(p, size).PadLeft(size) + "|" + Limit(n, size).PadRight(size));
                }

                cur = node.Next[0];
            }

            var s = new StringBuilder();

            for (var i = sbs.Length - 1; i >= 0; i--)
            {
                s.AppendLine(sbs[i].ToString());
            }

            return s;
        }

        private static string Limit(string text, int size)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length > size ? text.Substring(0, size) : text;
        }

    }

    #region Dump Extensions

    internal static class DumpExtensions
    {
        public static string Dump(this uint pageID)
        {
            return pageID == uint.MaxValue ? "----" : pageID.ToString("0000");
        }

        public static string Dump(this ushort index)
        {
            return index == ushort.MaxValue ? "--" : index.ToString();
        }

        public static string Dump(this PageAddress address)
        {
            return address.PageID.Dump() + ":" + address.Index.Dump();
        }

        public static string Dump(this BsonValue value)
        {
            return value.ToString();
        }

        public static void Dump(this BasePage page, StringBuilder sb)
        {
            if (page is HeaderPage) Dump((HeaderPage)page, sb);
            if (page is CollectionPage) Dump((CollectionPage)page, sb);
            if (page is IndexPage) Dump((IndexPage)page, sb);
            if (page is DataPage) Dump((DataPage)page, sb);
            if (page is ExtendPage) Dump((ExtendPage)page, sb);
        }

        public static void Dump(this HeaderPage page, StringBuilder sb)
        {
            sb.AppendFormat("Change: {0}, Version: {1}, FreeEmptyPageID: {2}, LastPageID: {3}, ColID: {4}",
                page.ChangeID,
                page.UserVersion,
                page.FreeEmptyPageID.Dump(),
                page.LastPageID.Dump(),
                page.FirstCollectionPageID.Dump());
        }

        public static void Dump(this IndexPage page, StringBuilder sb)
        {
            foreach (var node in page.Nodes.Values)
            {
                sb.AppendFormat("[{0}] Key: {1}, Data: {2} / ",
                    node.Position.Index,
                    node.Key.Dump(),
                    node.DataBlock.Dump());
            }
        }

        public static void Dump(this CollectionPage page, StringBuilder sb)
        {
            sb.AppendFormat("'{0}', Count: {1}, FreeDataPageID: {2}, Indexes = ",
                page.CollectionName,
                page.DocumentCount,
                page.FreeDataPageID.Dump());

            foreach (var i in page.GetIndexes(true))
            {
                sb.AppendFormat("[{0}] Field: '{1}', Head: {2}, FreeIndexPageID: {3} / ",
                    i.Slot,
                    i.Field,
                    i.HeadNode.Dump(),
                    i.FreeIndexPageID.Dump());
            }
        }

        public static void Dump(this DataPage page, StringBuilder sb)
        {
            foreach (var block in page.DataBlocks.Values)
            {
                sb.AppendFormat("[{0}] BytesUsed: {1}{2} / ",
                    block.Position.Index,
                    block.Data.Length,
                    block.ExtendPageID == uint.MaxValue ? "" : ", Ext: " + block.ExtendPageID.Dump());
            }
        }

        public static void Dump(this ExtendPage page, StringBuilder sb)
        {
            sb.AppendFormat("BytesUsed: {0}", page.Data.Length);
        }

        public static string PadBoth(this string str, int length)
        {
            int spaces = length - str.Length;
            int padLeft = spaces / 2 + str.Length;
            return str.PadLeft(padLeft).PadRight(length);
        }
    }

    #endregion
}
