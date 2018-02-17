using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Implement index operation less or equan than
    /// </summary>
    internal class IndexLess : Index
    {
        private BsonValue _value;
        private bool _equals;

        public BsonValue Value { get { return _value; } }
        public bool IsEquals { get { return _equals; } }

        public IndexLess(string name, BsonValue value, bool equals)
            : base(name)
        {
            _value = value;
            _equals = equals;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            foreach (var node in indexer.FindAll(index, Query.Ascending))
            {
                // compares only with are same type
                if (node.Key.Type == _value.Type || (node.Key.IsNumber && _value.IsNumber))
                {
                    var diff = node.Key.CompareTo(_value);

                    if (diff == 1 || (!_equals && diff == 0)) break;

                    if (node.IsHeadTail(index)) yield break;

                    yield return node;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("LT{0}({1})", this.Name, _equals ? "E" : "");
        }
    }
}