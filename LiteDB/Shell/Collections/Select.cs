using System;
using System.Collections.Generic;

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
            var expression = BsonExpression.ReadExpression(s, false);
            var output = expression == null ? this.ReadBsonValue(s) : null;

            // select command required output value, path or expression
            if (expression == null && output == null) throw LiteException.SyntaxError(s, "Missing select path");

            var query = Query.All();

            if (s.Scan(@"\s*where\s*").Length > 0)
            {
                query = this.ReadQuery(s, true);
            }

            var skipLimit = this.ReadSkipLimit(s);
            var includes = this.ReadIncludes(s);

            s.ThrowIfNotFinish();

            var docs = engine.Find(col, query, includes, skipLimit.Key, skipLimit.Value);
            var expr = expression == null ? null : new BsonExpression(expression);

            foreach(var doc in docs)
            {
                if (expr != null)
                {
                    foreach(var value in expr.Execute(doc, false))
                    {
                        yield return value;
                    }
                }
                else
                {
                    yield return output;
                }
            }
        }
    }
}