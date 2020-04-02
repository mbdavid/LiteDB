using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Try recovery data from current datafile into a new datafile.
        /// </summary>
        public static string Recovery(string filename)
        {
            // if not exists, just exit
            if (!File.Exists(filename)) return "";

            var log = new StringBuilder();
            var newfilename = FileHelper.GetTempFile(filename, "-recovery", true);
            var count = 0;

            using (var olddb = new LiteEngine(filename))
            using (var newdb = new LiteEngine(newfilename, false))
            {
                // get header from old database (this must must be possible to read)
                var header = olddb._pager.GetPage<HeaderPage>(0);

                var collections = RecoveryCollectionPages(olddb, header, log);

                // try recovery all data pages
                for (uint i = 1; i < header.LastPageID; i++)
                {
                    DataPage dataPage = null;

                    try
                    {
                        var buffer = olddb._disk.ReadPage(i);

                        // searching only for DataPage (PageType == 4)
                        if (buffer[4] != 4) continue;

                        dataPage = BasePage.ReadPage(buffer) as DataPage;
                    }
                    catch (Exception ex)
                    {
                        log.AppendLine($"Page {i} (DataPage) Error: {ex.Message}");
                        continue;
                    }

                    // try find collectionName using pageID map (use fixed name if not found)
                    if (collections.TryGetValue(i, out var colname) == false)
                    {
                        colname = "_recovery";
                    }

                    foreach (var block in dataPage.DataBlocks)
                    {
                        try
                        {
                            // read bytes
                            var bson = olddb._data.Read(block.Value.Position);

                            // deserialize as document
                            var doc = BsonSerializer.Deserialize(bson);

                            // and insert into new database
                            newdb.Insert(colname, doc);

                            count++;
                        }
                        catch (Exception ex)
                        {
                            log.AppendLine($"Document {block.Value.Position} Error: {ex.Message}");
                            continue;
                        }
                    }
                }
            }

            log.Insert(0, $"Document recovery count: {count}\n");

            return log.ToString();
        }

        private static Dictionary<uint, string> RecoveryCollectionPages(LiteEngine engine, HeaderPage header, StringBuilder log)
        {
            var result = new Dictionary<uint, string>();

            // get collection page
            foreach (var col in header.CollectionPages)
            {
                CollectionPage colPage = null;

                try
                {
                    // read collection page
                    var buffer = engine._disk.ReadPage(col.Value);
                    var page = BasePage.ReadPage(buffer);

                    if (page.PageType != PageType.Collection) continue;

                    colPage = page as CollectionPage;
                }
                catch (Exception ex)
                {
                    log.AppendLine($"Page {col.Value} (Collection) Error: {ex.Message}");
                    continue;
                }

                // get all pageID from all valid indexes
                var pagesID = new HashSet<uint>(colPage.Indexes.Where(x => x.IsEmpty == false && x.HeadNode.PageID != uint.MaxValue).Select(x => x.HeadNode.PageID));

                // load all dataPages from this initial index pageIDs
                var dataPages = RecoveryDetectCollectionByIndexPages(engine, pagesID, log);

                // populate resultset with this collection name/data page
                foreach(var page in dataPages)
                {
                    result[page] = col.Key;
                }
            }

            return result;
        }

        private static HashSet<uint> RecoveryDetectCollectionByIndexPages(LiteEngine engine, HashSet<uint> initialPagesID, StringBuilder log)
        {
            var indexPages = new Dictionary<uint, bool>();
            var dataPages = new HashSet<uint>();

            foreach(var pageID in initialPagesID)
            {
                indexPages.Add(pageID, false);
            }

            // discover all indexes pages related with this current indexPage (all of them are in same collection)
            while (indexPages.Count(x => x.Value == false) > 0)
            {
                var item = indexPages.First(x => x.Value == false);

                // mark page as readed
                indexPages[item.Key] = true;
                IndexPage indexPage = null;

                try
                {
                    // try read page from disk and deserialize as IndexPage
                    var buffer = engine._disk.ReadPage(item.Key);
                    var page = BasePage.ReadPage(buffer);

                    if (page.PageType != PageType.Index) continue;

                    indexPage = page as IndexPage;
                }
                catch(Exception ex)
                {
                    log.AppendLine($"Page {item.Key} (Collection) Error: {ex.Message}");
                    continue;
                }

                // now, check for all nodes to get dataPages
                foreach (var node in indexPage.Nodes.Values)
                {
                    if (node.DataBlock.PageID != uint.MaxValue)
                    {
                        dataPages.Add(node.DataBlock.PageID);
                    }

                    // add into indexPages all possible indexPages
                    if (!indexPages.ContainsKey(node.PrevNode.PageID) && node.PrevNode.PageID != uint.MaxValue)
                    {
                        indexPages.Add(node.PrevNode.PageID, false);
                    }

                    if (!indexPages.ContainsKey(node.NextNode.PageID) && node.NextNode.PageID != uint.MaxValue)
                    {
                        indexPages.Add(node.NextNode.PageID, false);
                    }

                    foreach (var pos in node.Prev.Where(x => !x.IsEmpty && x.PageID != uint.MaxValue))
                    {
                        if (!indexPages.ContainsKey(pos.PageID)) indexPages.Add(pos.PageID, false);
                    }

                    foreach (var pos in node.Next.Where(x => !x.IsEmpty && x.PageID != uint.MaxValue))
                    {
                        if (!indexPages.ContainsKey(pos.PageID)) indexPages.Add(pos.PageID, false);
                    }
                }
            }

            return dataPages;
        }
    }
}
