using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "update",
        Syntax = "db.<collection>.update <jsonDoc> | <field|path> [+]= <value|expression> [where <filter>]",
        Description = "Update a single document by _id value or a range of document using field=value pairs and where filter clause. If your field is an array, can be use += to add new value. Returns number of updated documents.",
        Examples = new string[] {
            "db.orders.update { _id: 1, customerName: \"John\" }",
            "db.orders.update name = \"John\" where _id = 22",
            "db.orders.update name = UPPER($.name), $.age = $.age + 1",
            "db.orders.update $.tags += \"blue\" where _id in [1, 2, 44]"
        }
    )]
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

                var updates = new List<UpdateData>();
                var query = Query.All();

                while(!s.HasTerminated)
                {
                    var path = BsonExpression.ReadExpression(s, true, true).Source;
                    var action = s.Scan(@"\s*\+?=\s*").Trim().ThrowIfEmpty("Invalid operator (support = or +=)", s);
                    var value = this.ReadBsonValue(s);
                    var expr = value == null ? BsonExpression.ReadExpression(s, true, false) : null;

                    if (action != "+=" && action != "=") throw LiteException.SyntaxError(s);
                    if (value == null && expr == null) throw LiteException.SyntaxError(s);

                    updates.Add(new UpdateData { Path = path, Value = value, Expr = expr, Add = action == "+=" });
                
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

                // fetch documents to update
                var count = engine.Update(col, this.FetchDocuments(engine, col, query, updates));

                yield return count;
            }
        }

        private IEnumerable<BsonDocument> FetchDocuments(LiteEngine engine, string col, Query query, List<UpdateData> updates)
        {
            // query document accord query and return modified only documents
            foreach (var doc in engine.Find(col, query))
            {
                var docChanged = false;

                foreach (var update in updates)
                {
                    var itemChanged = false;

                    if (update.Value == null)
                    {
                        itemChanged = doc.Set(update.Path, update.Expr, update.Add);
                    }
                    else
                    {
                        itemChanged = doc.Set(update.Path, update.Value, update.Add);
                    }

                    if (itemChanged) docChanged = true;
                }

                // execute update only if document was changed
                if (docChanged)
                {
                    yield return doc;
                }
            }
        }

        public class UpdateData
        {
            public string Path { get; set; }
            public BsonValue Value { get; set; }
            public BsonExpression Expr { get; set; }
            public bool Add { get; set; }
        }
    }
}