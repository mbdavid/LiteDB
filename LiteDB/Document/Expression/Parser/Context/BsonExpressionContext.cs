using LiteDB.Engine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB
{
    internal class BsonExpressionContext
    {
        public BsonExpressionContext(IEnumerable<BsonDocument> source, BsonDocument root, BsonValue current, Collation collation, BsonDocument parameters)
        {
            this.Source = source;
            this.Root = root;
            this.Current = current;
            this.Collation = collation;
            this.Parameters = parameters;
        }

        public IEnumerable<BsonDocument> Source { get; }
        public BsonDocument Root { get; }
        public BsonValue Current { get; }
        public Collation Collation { get; }
        public BsonDocument Parameters { get; }
    }
}
