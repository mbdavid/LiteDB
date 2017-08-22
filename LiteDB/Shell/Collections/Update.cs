using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    internal class CollectionUpdate : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "update");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);

            // single document update
            if(s.Match(@"\s*\{"))
            {
                var doc = JsonSerializer.Deserialize(s.ToString()).AsDocument;

                yield return engine.Update(col, doc);
            }
            // query update
            else
            {
                var query = Query.All();
                var updates = new List<Update>();

                while(!s.HasTerminated)
                {
                    var path = LiteExpression.Extract(s, true);
                    var action = s.Scan(@"\s*(\+)?=\s*", 1);
                    var expr = LiteExpression.Extract(s, false).TrimToNull();

                    updates.Add(new Update
                    {
                        Path = path,
                        Action = action == "+" ? UpdateAction.Add : UpdateAction.Set,
                        Value = expr == null ? JsonSerializer.Deserialize(s) : null,
                        Expression = expr != null ? new LiteExpression(expr) : null
                    });

                    s.Scan(@"\s*");

                    if (s.Scan(@",\s*").Length > 0) continue;
                    else if(s.Scan(@"where\s*").Length > 0 || s.HasTerminated) break;
                    else throw LiteException.SyntaxError("Invalid update shell command syntax");
                }

                if(!s.HasTerminated)
                {
                    query = this.ReadQuery(s);
                }

                yield return engine.Update(col, query, updates.ToArray());
            }
        }
    }
}