using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Represent a single page that contains all collections names and link to CollectionPage. It's fixed page 1 and are created on database initialization
    /// </summary>
    internal class CollectionListPage : BasePage
    {
        /// <summary>
        /// Represent maximum bytes that all collections names can be used in collection list page
        /// </summary>
        public const ushort MAX_COLLECTIONS_SIZE = 7500;

        /// <summary>
        /// Page type = Transaction
        /// </summary>
        public override PageType PageType { get { return PageType.CollectionList; } }

        /// <summary>
        /// Database user version (this property was moved to collection list page because Header page now are single instance with no locks)
        /// </summary>
        public int UserVersion { get; set; }

        /// <summary>
        /// Get a dictionary with all collection pages with pageID link
        /// </summary>
        private Dictionary<string, uint> _collectionPages;

        private CollectionListPage()
        {
        }

        public CollectionListPage(uint pageID)
            : base(pageID)
        {
            this.ItemCount = 0;
            this.UserVersion = 0;

            _collectionPages = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }

        public uint? GetPageID(string name)
        {
            return _collectionPages.TryGetValue(name, out var pageID) ? (uint?)pageID : null;
        }

        public IEnumerable<KeyValuePair<string, uint>> GetAll()
        {
            return _collectionPages.ToList();
        }

        public void Add(string name, uint pageID)
        {
            // check limit count (8 bytes per collection = 4 to string length, 4 for uint pageID)
            var sum = _collectionPages.Sum(x => x.Key.Length + 8) + (name.Length + 8);

            if (sum >= MAX_COLLECTIONS_SIZE)
            {
                throw LiteException.CollectionLimitExceeded(MAX_COLLECTIONS_SIZE);
            }

            _collectionPages.Add(name, pageID);

            this.ItemCount++;
        }

        public void Rename(string oldName, string newName)
        {
            // check limit count (8 bytes per collection = 4 to string length, 4 for uint pageID)
            var sum = _collectionPages
                .Where(x => x.Key != oldName)
                .Sum(x => x.Key.Length + 8) + (newName.Length + 8);

            if (sum >= MAX_COLLECTIONS_SIZE)
            {
                throw LiteException.CollectionLimitExceeded(MAX_COLLECTIONS_SIZE);
            }

            var pageID = _collectionPages[oldName];

            _collectionPages.Remove(oldName);
            _collectionPages.Add(newName, pageID);
        }

        public void Delete(string name)
        {
            _collectionPages.Remove(name);

            this.ItemCount--;
        }

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
            this.UserVersion = reader.ReadInt32();

            for (var i = 0; i < this.ItemCount; i++)
            {
                _collectionPages.Add(reader.ReadString(), reader.ReadUInt32());
            }
        }

        protected override void WriteContent(ByteWriter writer)
        {
            writer.Write(this.UserVersion);

            foreach (var col in _collectionPages)
            {
                writer.Write(col.Key);
                writer.Write(col.Value);
            }
        }

        public override BasePage Clone()
        {
            return new CollectionListPage
            {
                // base page
                PageID = this.PageID,
                PrevPageID = this.PrevPageID,
                NextPageID = this.NextPageID,
                ItemCount = this.ItemCount,
                FreeBytes = this.FreeBytes,
                TransactionID = this.TransactionID,
                // collection list page
                UserVersion = this.UserVersion,
                _collectionPages = new Dictionary<string, uint>(_collectionPages, StringComparer.OrdinalIgnoreCase)
            };
        }

        #endregion
    }
}