using System;
using System.Collections.Generic;
using LiteDB;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    class Dump
    {
        public static void Info(string text)
        {
            Debug.Print(string.Format("== [ {0} ] ", text).PadRight(120, '='));
        }

        public static void Pages(LiteEngine db, string text = "Page Dump")
        {
            var sb = new StringBuilder();

            Info(text);

            for (uint i = 0; i <= db.Cache.Header.LastPageID; i++)
            {
                var p = i == 0 ? db.Cache.Header : db.Pager.GetPage<BasePage>(i);

                sb.AppendFormat("{0} <{1},{2}> [{3}] {4}:{5} | ",
                    p.PageID.Dump(),
                    p.PrevPageID.Dump(),
                    p.NextPageID.Dump(),
                    p.PageType.ToString().PadRight(6).Substring(0, 6),
                    p.IsDirty ? "D" : "-",
                    p.FreeBytes.ToString("0000"));

                if (p.PageType == PageType.Collection) p = db.Pager.GetPage<CollectionPage>(i);
                if (p.PageType == PageType.Data) p = db.Pager.GetPage<DataPage>(i);
                if (p.PageType == PageType.Extend) p = db.Pager.GetPage<ExtendPage>(i);
                if (p.PageType == PageType.Index) p = db.Pager.GetPage<IndexPage>(i);

                p.Dump(sb);
                sb.AppendLine();
            }

            Debug.Print(sb.ToString());
        }

        public static void Index(LiteEngine db, CollectionIndex index, int size = 5)
        {
            var sbs = new StringBuilder[IndexNode.MAX_LEVEL_LENGTH + 1];
            var first = true;

            for (var i = 0; i < sbs.Length; i++)
            {
                sbs[i] = new StringBuilder();
            }

            var cur = index.HeadNode;

            while (!cur.IsEmpty)
            {
                var page = db.Pager.GetPage<IndexPage>(cur.PageID);
                var node = page.Nodes[cur.Index];

                sbs[0].Append((first ? "HEAD" :
                    node.Key.Value == null ? "null" : node.Key.Value.ToString()).PadBoth(1 + (2 * size)));

                first = false;

                for (var i = 0; i < IndexNode.MAX_LEVEL_LENGTH; i++)
                {
                    var sb = sbs[i + 1];
                    var p = "-";
                    var n = "-";

                    if (i < node.Prev.Length)
                    {
                        if (!node.Prev[i].IsEmpty)
                        {
                            if (node.Prev[i].Equals(index.HeadNode))
                            {
                                p = "<-H";
                            }
                            else
                            {
                                var pprev = db.Pager.GetPage<IndexPage>(node.Prev[i].PageID);
                                var pnode = pprev.Nodes[node.Prev[i].Index];
                                p = pnode.Key.Value == null ? "null" : pnode.Key.Value.ToString();
                            }
                        }
                        if (!node.Next[i].IsEmpty)
                        {
                            var pnext = db.Pager.GetPage<IndexPage>(node.Next[i].PageID);
                            var pnode = pnext.Nodes[node.Next[i].Index];
                            n = pnode.Key.Value == null ? "null" : pnode.Key.Value.ToString();
                        }
                    }

                    sb.Append(p.PadLeft(size) + "|" + n.PadRight(size));
                }

                cur = node.Next[0];
            }

            for (var i = sbs.Length - 1; i >= 0; i--)
            {
                Debug.Print(sbs[i].ToString());
            }
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

        public static string Dump(this IndexKey value)
        {
            return string.Format("{0}{1}{0}", value.Type == IndexDataType.String ? "'" : "", value.ToString());
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
            sb.AppendFormat("Change: {0}, FreeEmptyPageID: {1}, LastPageID: {2}",
                page.ChangeID,
                page.FreeEmptyPageID.Dump(),
                page.LastPageID.Dump());
        }

        public static void Dump(this IndexPage page, StringBuilder sb)
        {
            foreach (var node in page.Nodes.Values)
            {
                sb.AppendFormat("Idx: {0}, Key: {1}, Data: {2} / ",
                    node.Position.Index,
                    node.Key.Dump(),
                    node.DataBlock.Dump());
            }
        }

        public static void Dump(this CollectionPage page, StringBuilder sb)
        {
            sb.AppendFormat("Name: '{0}', DocumentCount: {1}, FreeDataPageID: {2}, Indexes > ",
                page.CollectionName,
                page.DocumentCount,
                page.FreeDataPageID.Dump());

            var idx = 0;

            foreach (var i in page.Indexes)
            {
                if (i.IsEmpty) continue;

                sb.AppendFormat("Idx: {0}, Field: '{1}', Head: {2}, FreeIndexPageID: {3} / ",
                    idx,
                    i.Field,
                    i.HeadNode.Dump(),
                    i.FreeIndexPageID.Dump());

                idx++;
            }
        }

        public static void Dump(this DataPage page, StringBuilder sb)
        {
            foreach (var block in page.DataBlocks.Values)
            {
                sb.AppendFormat("Idx: {0}, BytesUsed: {1}{2} / ",
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
