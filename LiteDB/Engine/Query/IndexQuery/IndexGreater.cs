using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Implement index greater or equal than operation
    /// </summary>
    internal class IndexGreater : Index
    {
        private BsonValue _value;
        private bool _equals;

        public BsonValue Value { get { return _value; } }
        public bool IsEquals { get { return _equals; } }

        public IndexGreater(string name, BsonValue value, bool equals)
            : base(name)
        {
            _value = value;
            _equals = equals;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            // find first indexNode
            var node = indexer.Find(index, _value, true, Query.Ascending);

            if (node == null) yield break;

            // move until next is last
            while (node != null)
            {
                // compares only with are same type
                if (node.Key.Type == _value.Type || (node.Key.IsNumber && _value.IsNumber))
                {
                    var diff = node.Key.CompareTo(_value);

                    if (diff == 1 || (_equals && diff == 0))
                    {
                        if (node.IsHeadTail(index)) yield break;

                        yield return node;
                    }
                }

                node = indexer.GetNode(node.Next[0]);
            }
        }

        public override string ToString()
        {
            return string.Format("GT{0}({1})", this.Name, _equals ? "E" : "");
        }
    }
}