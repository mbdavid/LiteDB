using System;
using System.Text;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class CollectionIndex
    {
        /// <summary>
        /// Slot index [0-255] used in all index nodes
        /// </summary>
        public byte Slot { get; }

        /// <summary>
        /// Indicate index type: 0 = SkipList (reserved for future use)
        /// </summary>
        public byte IndexType { get; }

        /// <summary>
        /// Index name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get index expression (path or expr)
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Get BsonExpression from Expression
        /// </summary>
        public BsonExpression BsonExpr { get; }

        /// <summary>
        /// Indicate if this index has distinct values only
        /// </summary>
        public bool Unique { get; }

        /// <summary>
        /// Head page address for this index
        /// </summary>
        public PageAddress Head { get; set; }

        /// <summary>
        /// A link pointer to tail node
        /// </summary>
        public PageAddress Tail { get; set; }

        /// <summary>
        /// Get/Set collection max level
        /// </summary>
        public byte MaxLevel { get; set; } = 1;

        /// <summary>
        /// Counter of keys in this index
        /// </summary>
        public uint KeyCount { get; set; } = 0;

        /// <summary>
        /// Counter of unique keys in this index (online but be dirty on delete index nodes... will fix on next analyze)
        /// </summary>
        public uint UniqueKeyCount { get; set; } = 0;

        /// <summary>
        /// Free index page linked-list (N lists for different range of FreeBlocks)
        /// </summary>
        public uint[] FreeIndexPageList { get; } = new uint[PAGE_FREE_LIST_SLOTS];

        /// <summary>
        /// Get index density based on KeyCount vs UniqueKeyCount. Value are from 0 to 1.
        /// 0 means completed unique keys (best)
        /// 1 means has only 1 single unique key in all index (worst)
        /// </summary>
        public double Density
        {
            get
            {
                if (this.Unique) return 0;
                if (this.UniqueKeyCount == 0 || this.KeyCount == 0) return 1;

                var density = (double)Math.Min(this.UniqueKeyCount, this.KeyCount) /
                    (double)this.KeyCount;

                return Math.Round(density, 2);
            }
        }

        /// <summary>
        /// Returns if this index slot is empty and can be used as new index
        /// </summary>
        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Name); }
        }

        public CollectionIndex(byte slot, byte indexType, string name, string expr, bool unique)
        {
            this.Slot = slot;
            this.IndexType = indexType;
            this.Name = name;
            this.Expression = expr;
            this.Unique = unique;

            this.BsonExpr = BsonExpression.Create(expr);

            for (var i = 0; i < PAGE_FREE_LIST_SLOTS; i++)
            {
                this.FreeIndexPageList[i] = uint.MaxValue;
            }
        }

        public CollectionIndex(BufferReader reader)
        {
            this.Slot = reader.ReadByte();
            this.IndexType = reader.ReadByte();
            this.Name = reader.ReadCString();
            this.Expression = reader.ReadCString();
            this.Unique = reader.ReadBoolean();
            this.Head = reader.ReadPageAddress(); // 5
            this.Tail = reader.ReadPageAddress(); // 5
            this.MaxLevel = reader.ReadByte(); // 1
            this.KeyCount = reader.ReadUInt32(); // 4
            this.UniqueKeyCount = reader.ReadUInt32(); // 4

            this.BsonExpr = BsonExpression.Create(this.Expression);

            for (var i = 0; i < PAGE_FREE_LIST_SLOTS; i++)
            {
                this.FreeIndexPageList[i] = reader.ReadUInt32();
            }
        }

        public void UpdateBuffer(BufferWriter writer)
        {
            writer.Write(this.Slot);
            writer.Write(this.IndexType);
            writer.WriteCString(this.Name);
            writer.WriteCString(this.Expression);
            writer.Write(this.Unique);
            writer.Write(this.Head);
            writer.Write(this.Tail);
            writer.Write(this.MaxLevel);
            writer.Write(this.KeyCount);
            writer.Write(this.UniqueKeyCount);

            for (var i = 0; i < PAGE_FREE_LIST_SLOTS; i++)
            {
                writer.Write(this.FreeIndexPageList[i]);
            }
        }

        /// <summary>
        /// Get index collection size used in CollectionPage
        /// </summary>
        public static int GetLength(CollectionIndex index)
        {
            return GetLength(index.Name, index.Expression);
        }

        /// <summary>
        /// Get index collection size used in CollectionPage
        /// </summary>
        public static int GetLength(string name, string expr)
        {
            return
                1 + // Slot
                1 + // IndexType
                Encoding.UTF8.GetByteCount(name) + 1 + // Name + \0
                Encoding.UTF8.GetByteCount(expr) + 1 + // Expression + \0
                1 + // Unique
                PageAddress.SIZE + // Head
                PageAddress.SIZE + // Tail
                1 + // MaxLevel
                4 + // KeyCount
                4 + // UniqueKeyCount
                (PAGE_FREE_LIST_SLOTS * PageAddress.SIZE); // FreeListPage
        }
    }
}