using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    internal class Select : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "select");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);

            // try read any kind of expression
            var expression = BsonExpression.ReadExpression(s, false, false);
                
            // if not found a valid one, try read only as path (will add $. before)
            if (expression == null)
            {
                expression = BsonExpression.ReadExpression(s, true, true);
            }

            var query = Query.All();

            if (s.Scan(@"\s*where\s*").Length > 0)
            {
                query = this.ReadQuery(s, true);
            }

            var skipLimit = this.ReadSkipLimit(s);
            var includes = this.ReadIncludes(s);

            s.ThrowIfNotFinish();

            var docs = engine.Find(col, query, includes, skipLimit.Key, skipLimit.Value);

            foreach(var doc in docs)
            {
                foreach (var value in expression.Execute(doc, false))
                {
                    yield return value;
                }
            }
        }
    }
}