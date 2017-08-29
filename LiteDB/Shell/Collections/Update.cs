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

                s.ThrowIfNotFinish();

                yield return engine.Update(col, doc);
            }
            // query update
            else
            {
                // db.colName.update 
                //     field = value, 
                //     array += valueToAdd,
                // where _id = 1 
                //   and ...

                var query = Query.All();
                var updates = new Update();

                while(!s.HasTerminated)
                {
                    var path = BsonExpression.ReadExpression(s, true, true).Source;
                    var action = s.Scan(@"\s*(\+)?=\s*", 1).ThrowIfEmpty("Invalid operator (support = or +=)", s);
                    var value = this.ReadBsonValue(s);
                    var expr = value == null ? BsonExpression.ReadExpression(s, true, false)?.Source : null;

                    if (action == "+" && value != null)
                    {
                        updates.Add(path, value);
                    }
                    else if (action == "+" && expr != null)
                    {
                        updates.AddExpr(path, expr);
                    }
                    else if (action == "" && value != null)
                    {
                        updates.Set(path, value);
                    }
                    else if (action == "" && expr != null)
                    {
                        updates.SetExpr(path, expr);
                    }
                    else
                    {
                        throw LiteException.SyntaxError(s);
                    }

                    s.Scan(@"\s*");

                    if (s.Scan(@",\s*").Length > 0) continue;
                    else if(s.Scan(@"where\s*").Length > 0 || s.HasTerminated) break;
                    else throw LiteException.SyntaxError(s);
                }

                if(!s.HasTerminated)
                {
                    query = this.ReadQuery(s, false);
                }

                s.ThrowIfNotFinish();

                yield return engine.Update(col, query, updates);
            }
        }
    }
}