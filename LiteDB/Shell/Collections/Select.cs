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
            var fields = new Dictionary<string, BsonExpression>();
            var index = 0;

            // read all fields definitions (support AS as keyword no name field)
            while(!s.HasTerminated)
            {
                var expression = BsonExpression.ReadExpression(s, false);
                var key = s.Scan(@"\s*as\s+(\w+)", 1).TrimToNull()
                    ?? ("expr" + (++index));

                fields.Add(key, new BsonExpression(expression));

                if (s.Scan(@"\s*,\s*").Length > 0) continue;
                break;
            }

            // select command required output value, path or expression
            if (fields.Count == 0) throw LiteException.SyntaxError(s, "Missing select path");

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
                // if is a single value, return as just field
                if (fields.Count == 1)
                {
                    foreach (var value in fields.Values.First().Execute(doc, false))
                    {
                        yield return value;
                    }
                }
                else
                {
                    var output = new BsonDocument();

                    foreach (var field in fields)
                    {
                        output[field.Key] = field.Value.Execute(doc, true).First();
                    }

                    yield return output;
                }
            }
        }
    }
}