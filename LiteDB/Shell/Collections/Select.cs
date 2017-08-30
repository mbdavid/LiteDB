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
                // try read any kind of expression
                var expression = BsonExpression.ReadExpression(s, false, false);

                // if not found a valid one, try read only as path (will add $. before)
                if (expression == null)
                {
                    expression = BsonExpression.ReadExpression(s, true, true);
                }

                var key = s.Scan(@"\s*as\s+([\w-]+)", 1).TrimToNull()
                    ?? this.NamedField(expression)
                    ?? ("expr" + (++index));

                // if key already exits, add with another name
                while (fields.ContainsKey(key))
                {
                    key = "expr" + (++index);
                }

                fields.Add(key, expression);

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

        private string NamedField(BsonExpression expr)
        {
            var segments = expr.Source.Split('.');

            return Regex.Replace(segments[segments.Length - 1], @"(\w+).*", "$1").TrimToNull();
        }
    }
}