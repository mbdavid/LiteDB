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
        /// Get a dictionary with all collection pages with pageID link
        /// </summary>
        private Dictionary<string, uint> _collectionPages { get; set; }

        public CollectionListPage()
            : base(1)
        {
            _collectionPages = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

            this.ItemCount = 0;
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
            if (_collectionPages.Sum(x => x.Key.Length + 8) + name.Length + 8 >= MAX_COLLECTIONS_SIZE)
            {
                throw LiteException.CollectionLimitExceeded(MAX_COLLECTIONS_SIZE);
            }

            _collectionPages.Add(name, pageID);

            this.ItemCount++;
        }

        public void Rename(string oldName, string newName)
        {
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
            for (var i = 0; i < this.ItemCount; i++)
            {
                _collectionPages.Add(reader.ReadString(), reader.ReadUInt32());
            }
        }

        protected override void WriteContent(ByteWriter writer)
        {
            foreach (var col in _collectionPages)
            {
                writer.Write(col.Key);
                writer.Write(col.Value);
            }
        }

        #endregion
    }
}