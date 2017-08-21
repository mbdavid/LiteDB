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

                while(!s.HasTerminated && !s.Match(@"where\s+"))
                {
                    var path = LiteExpression.Extract(s);
                    s.Scan(@"\s*=\s*");
                    var expr = LiteExpression.Extract(s);

                    if(!string.IsNullOrEmpty(expr))
                    {
                        updates.Add(Update.Expr(path, expr));
                    }
                    else
                    {
                        updates.Add(Update.Value(path, JsonSerializer.Deserialize(s)));
                    }

                    s.Scan(@"\s*");
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