using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Do many database checks to verify if data file are consistent
        /// </summary>
        public bool CheckIntegrity(TextWriter writer)
        {
            _locker.EnterReserved(true);

            _disk.Queue.Wait();

            var time = Stopwatch.StartNew();

            writer.WriteLine("LiteDB Check Integrity Report");
            writer.WriteLine("=============================");

            var result = new List<bool>();
            var linkedPages = new List<uint>(new uint[] { 0 });

            result.Add(this.Run(writer, "Data file", "{0}", () => _disk.GetName(FileOrigin.Data) + " (" + FileHelper.FormatFileSize(_disk.GetLength(FileOrigin.Data)) + ")"));
            result.Add(this.Run(writer, "Log file", "{0}", () => _disk.GetName(FileOrigin.Log) + " (" + FileHelper.FormatFileSize(_disk.GetLength(FileOrigin.Log)) + ")"));
            result.Add(this.Run(writer, "Clear cache memory", "OK ({0} pages)", () => _disk.Cache.Clear()));
            result.Add(this.Run(writer, "Verify free empty list", "OK ({0} pages)", () => this.VerifyFreeEmptyList(linkedPages)));
            result.Add(this.Run(writer, "Verify data pages links", "OK ({0} pages)", () => this.VerifyDataPagesFreeList(linkedPages)));
            result.Add(this.Run(writer, "Verify index pages links", "OK ({0} pages)", () => this.VerifyIndexPagesFreeList(linkedPages)));
            result.Add(this.Run(writer, "Verify all linked pages", "OK ({0} pages)", () => this.VerifyAllPagesList(linkedPages)));
            result.Add(this.Run(writer, "Verify index nodes", "OK ({0} nodes)", () => this.VerifyIndexNodes()));
            result.Add(this.Run(writer, "Verify data blocks", "OK ({0} blocks)", () => this.VerifyDataBlocks()));
            result.Add(this.Run(writer, "Verify documents", "OK ({0} documents)", () => this.VerifyDataBlocks()));
            result.Add(this.Run(writer, "Total time elapsed", "{0}", () => time.Elapsed));

            _locker.ExitReserved(true);

            return result.All(x => x);
        }

        private bool Run(TextWriter writer, string title, string ok, Func<object> action)
        {
            var text = title.PadRight(28, '.') + ": ";
            var result = true;

            writer.Write(text);

            try
            {
                var r = action();
                writer.WriteLine(string.Format(ok, r));
            }
            catch (Exception ex)
            {
                result = false;
                writer.WriteLine("ERR: " + ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Check if all free empty list pages are OK
        /// </summary>
        private int VerifyFreeEmptyList(List<uint> linkedPages)
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

                    linkedPages.Add(page.PageID);

                    next = page.NextPageID;

                    transaction.Safepoint();

                    counter++;
                }

                return counter;
            });
        }

        /// <summary>
        /// Checks if all data pages are in free list
        /// </summary>
        private int VerifyDataPagesFreeList(List<uint> linkedPages)
        {
            return this.AutoTransaction(transaction =>
            {
                var counter = 0;

                foreach (var col in _header.GetCollections())
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Read, col.Key, false);

                    linkedPages.Add(col.Value);

                    for (var slot = 0; slot < PAGE_FREE_LIST_SLOTS; slot++)
                    {
                        var next = snapshot.CollectionPage.FreeDataPageList[slot];

                        while (next != uint.MaxValue)
                        {
                            var page = snapshot.GetPage<BasePage>(next);

                            if (page.PageType != PageType.Data)
                            {
                                throw new LiteException(0, $"Page {page.PageID} should be DataPage type");
                            }

                            linkedPages.Add(page.PageID);

                            counter++;
                            next = page.NextPageID;
                            transaction.Safepoint();
                        }
                    }
                }

                return counter;
            });
        }

        /// <summary>
        /// Checks if all index pages are in free list
        /// </summary>
        private int VerifyIndexPagesFreeList(List<uint> linkedPages)
        {
            return this.AutoTransaction(transaction =>
            {
                var counter = 0;

                foreach (var col in _header.GetCollections())
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Read, col.Key, false);

                    foreach (var index in snapshot.CollectionPage.GetCollectionIndexes())
                    {
                        for (var slot = 0; slot < PAGE_FREE_LIST_SLOTS; slot++)
                        {
                            var next = index.FreeIndexPageList[slot];

                            while (next != uint.MaxValue)
                            {
                                var page = snapshot.GetPage<BasePage>(next);

                                if (page.PageType != PageType.Index)
                                {
                                    throw new LiteException(0, $"Page {page.PageID} should be IndexPage type");
                                }

                                linkedPages.Add(page.PageID);

                                counter++;
                                next = page.NextPageID;
                                transaction.Safepoint();
                            }
                        }
                    }
                }

                return counter;
            });
        }

        /// <summary>
        /// Check if all pages are linked 
        /// </summary>
        private int VerifyAllPagesList(List<uint> linkedPages)
        {
            linkedPages.Sort();

            for(var i = 0; i <= _header.LastPageID; i++)
            {
                if (linkedPages[i] != i)
                {
                    throw new LiteException(0, $"Invalid linked page sort order. PageID {linkedPages[i]} should be {i}");
                }
            }

            return (int)_header.LastPageID;
        }

        /// <summary>
        /// Check for all nodes pointers
        /// </summary>
        public int VerifyIndexNodes()
        {
            return this.AutoTransaction(transaction =>
            {
                var counter = 0;

                //foreach(var col in _header.GetCollections())
                //{
                //    var snapshot = transaction.CreateSnapshot(LockMode.Read, col.Key, false);

                //    var indexSlots = new HashSet<byte>(snapshot.CollectionPage.GetCollectionIndexes().Select(x => x.Slot));

                //    for (var pageSlot = 0; pageSlot < PAGE_FREE_LIST_SLOTS; pageSlot++)
                //    {
                //        var next = snapshot.CollectionPage.FreeIndexPageID[pageSlot];

                //        while (next != uint.MaxValue)
                //        {
                //            var page = snapshot.GetPage<IndexPage>(next);

                //            var nodes = page.GetIndexNodes().ToArray();

                //            foreach(var node in nodes)
                //            {
                //                if (!indexSlots.Contains(node.Slot))
                //                {
                //                    throw new LiteException(0, $"Invalid index slot in this IndexNode: {node.Key} [{node.Position}]");
                //                }

                //                // head/tail
                //                if (node.Key.IsMaxValue || node.Key.IsMinValue)
                //                {
                //                    counter++;
                //                    continue;
                //                }

                //                var dataPage = snapshot.GetPage<BasePage>(node.DataBlock.PageID);

                //                if (dataPage.PageType != PageType.Data)
                //                {
                //                    throw new LiteException(0, $"Invalid page type on index node data block point: {node.DataBlock}");
                //                }

                //                LookupNode(node.NextNode, snapshot, null, 0);

                //                for (var i = 0; i < node.Level; i++)
                //                {
                //                    LookupNode(node.Prev[i], snapshot, node.Key, -1);
                //                    LookupNode(node.Next[i], snapshot, node.Key, +1);
                //                }

                //                counter++;
                //            }

                //            next = page.NextPageID;
                //            transaction.Safepoint();
                //        }
                //    }
                //}

                return counter;
            });

            void LookupNode(PageAddress pageAddress, Snapshot snapshot, BsonValue key, int order)
            {
                if (pageAddress.IsEmpty) return;

                var page = snapshot.GetPage<IndexPage>(pageAddress.PageID);
                var node = page.GetIndexNode(pageAddress.Index);

                if (key == null) return;

                if (order == 1)
                {
                    if (!(node.Key >= key)) throw new LiteException(0, $"Node {pageAddress} `{node.Key}` should be greater than `{key}`");
                }
                else if (order == -1)
                {
                    if (!(node.Key <= key)) throw new LiteException(0, $"Node {pageAddress} `{node.Key}` should be less than `{key}`");
                }
            }
        }

        /// <summary>
        /// Check for all data blocks
        /// </summary>
        public int VerifyDataBlocks()
        {
            return this.AutoTransaction(transaction =>
            {
                var counter = 0;

                foreach (var col in _header.GetCollections())
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Read, col.Key, false);
                    var data = new DataService(snapshot);

                    for (var slot = 0; slot < PAGE_FREE_LIST_SLOTS; slot++)
                    {
                        var next = snapshot.CollectionPage.FreeDataPageList[slot];

                        while (next != uint.MaxValue)
                        {
                            var page = snapshot.GetPage<DataPage>(next);

                            foreach (var address in page.GetBlocks(false))
                            {
                                var block = page.GetBlock(address.Index);

                                counter++;
                            }

                            next = page.NextPageID;
                            transaction.Safepoint();
                        }
                    }
                }

                return counter;
            });

        }

        /// <summary>
        /// Check for all document and deserialize to test if ok (use PK index)
        /// </summary>
        public int VerifyDocuments()
        {
            return this.AutoTransaction(transaction =>
            {
                var counter = 0;

                foreach (var col in _header.GetCollections())
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Read, col.Key, false);
                    var data = new DataService(snapshot);

                    for (var slot = 0; slot < PAGE_FREE_LIST_SLOTS; slot++)
                    {
                        var next = snapshot.CollectionPage.FreeDataPageList[slot];

                        while (next != uint.MaxValue)
                        {
                            var page = snapshot.GetPage<DataPage>(next);

                            foreach (var block in page.GetBlocks(true))
                            {
                                using (var r = new BufferReader(data.Read(block)))
                                {
                                    var doc = r.ReadDocument();

                                    counter++;
                                }
                            }

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