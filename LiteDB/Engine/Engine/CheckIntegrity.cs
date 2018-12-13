using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Do many database checks to verify if data file are consistent
        /// </summary>
        public string CheckIntegrity()
        {
            _locker.EnterReserved(true);

            _disk.Queue.Wait();
            _disk.Cache.Clear();

            var report = new StringBuilder();
            var time = Stopwatch.StartNew();

            report.AppendLine("LiteDB Check Integrity Report");
            report.AppendLine("=============================");

            Run("Data file", "{0}", () => _disk.GetName(FileOrigin.Data) + " (" + FileHelper.FormatFileSize(_disk.GetLength(FileOrigin.Data)) + ")");
            Run("Log file", "{0}", () => _disk.GetName(FileOrigin.Log) + " (" + FileHelper.FormatFileSize(_disk.GetLength(FileOrigin.Log)) + ")");
            Run("Verify CRC data file", "OK ({0} pages)", () => this.VerifyPageCRC(FileOrigin.Data));
            Run("Verify CRC log file", "OK ({0} pages)", () => this.VerifyPageCRC(FileOrigin.Log));
            Run("Verify free empty list", "OK ({0} pages)", () => this.VerifyFreeEmptyList());
            Run("Verify data pages links", "OK ({0} pages)", () => this.VerifyPagesType(PageType.Data));
            Run("Verify index pages links", "OK ({0} pages)", () => this.VerifyPagesType(PageType.Index));
            Run("Verify index nodes", "OK ({0} nodes)", () => this.VerifyIndexNodes());
            Run("Verify data blocks", "OK ({0} documents)", () => this.VerifyDataBlocks());
            Run("Total time elapsed", "{0}", () => time.Elapsed);

            void Run(string title, string ok, Func<object> action)
            {
                report.Append(title.PadRight(28, '.') + ": ");

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

            return report.ToString();
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

        /// <summary>
        /// Check for all nodes pointers
        /// </summary>
        public int VerifyIndexNodes()
        {
            return this.AutoTransaction(transaction =>
            {
                var counter = 0;

                foreach(var col in _header.GetCollections())
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Read, col.Key, false);

                    for (var slot = 0; slot < 5; slot++)
                    {
                        var next = snapshot.CollectionPage.FreeIndexPageID[slot];

                        while (next != uint.MaxValue)
                        {
                            var page = snapshot.GetPage<IndexPage>(next);

                            var nodes = page.GetNodes();

                            foreach(var node in nodes)
                            {
                                if (node.Key.IsMaxValue || node.Key.IsMinValue) continue;

                                var dataPage = snapshot.GetPage<BasePage>(node.DataBlock.PageID);

                                if (dataPage.PageType != PageType.Data)
                                {
                                    throw new LiteException(0, $"Invalid page type on index node data block point: {node.DataBlock}");
                                }

                                LookupNode(node.PrevNode, snapshot, null, null);
                                LookupNode(node.NextNode, snapshot, null, null);

                                for (var i = 0; i < node.Level; i++)
                                {
                                    LookupNode(node.Prev[i], snapshot, node.Key, "<");
                                    LookupNode(node.Next[i], snapshot, node.Key, ">");
                                }

                                counter++;
                            }

                            next = page.NextPageID;
                            transaction.Safepoint();
                        }
                    }
                }

                return counter;
            });

            void LookupNode(PageAddress pageAddress, Snapshot snapshot, BsonValue key, string compare)
            {
                if (pageAddress.IsEmpty) return;

                var page = snapshot.GetPage<IndexPage>(pageAddress.PageID);
                var node = page.ReadNode(pageAddress.Index);

                if (key == null) return;

                if (compare == ">")
                {
                    if (!(node.Key > key)) throw new LiteException(0, $"Node {pageAddress} `{node.Key}` should be greater than `{key}`");
                }
                else if (compare == "<")
                {
                    if (!(node.Key < key)) throw new LiteException(0, $"Node {pageAddress} `{node.Key}` should be less than `{key}`");
                }
            }
        }

        /// <summary>
        /// Check for all data blocks and deserialize all documents
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

                    for (var slot = 0; slot < 5; slot++)
                    {
                        var next = snapshot.CollectionPage.FreeDataPageID[slot];

                        while (next != uint.MaxValue)
                        {
                            var page = snapshot.GetPage<DataPage>(next);

                            foreach (var block in page.GetBlocks())
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