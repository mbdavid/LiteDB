using System.Collections.Generic;

namespace LiteDB.Interfaces
{
   public interface IIndexService
   {
      /// <summary>
      /// Create a new index and returns head page address (skip list)
      /// </summary>
      CollectionIndex CreateIndex(CollectionPage col);

      /// <summary>
      /// Insert a new node index inside an collection index. Flip coin to know level
      /// </summary>
      IndexNode AddNode(CollectionIndex index, BsonValue key);

      /// <summary>
      /// Delete indexNode from a Index  ajust Next/Prev nodes
      /// </summary>
      void Delete(CollectionIndex index, PageAddress nodeAddress);

      /// <summary>
      /// Drop all indexes pages
      /// </summary>
      void DropIndex(CollectionIndex index);

      /// <summary>
      /// Get a node inside a page using PageAddress - Returns null if address IsEmpty
      /// </summary>
      IndexNode GetNode(PageAddress address);

      /// <summary>
      /// Flip coin - skip list - returns level node (start in 1)
      /// </summary>
      byte FlipCoin();

      IEnumerable<IndexNode> FindAll(CollectionIndex index, int order);

      /// <summary>
      /// Find first node that index match with value. If not found but sibling = true, returns near node (only non-unique index)
      /// Before find, value must be normalized
      /// </summary>
      IndexNode Find(CollectionIndex index, BsonValue value, bool sibling, int order);
   }

}