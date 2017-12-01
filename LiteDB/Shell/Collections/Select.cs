using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "select",
        Syntax = "db.<collection>.select <expression|path> [into <new_collection> [id:<data-type>]] [where <filter>] [includes <path1>,<path2>,...<pathN>] [skip N] [limit <M>]",
        Description = "Search for document using filter. Support document transforms using expression (see `help expression`). Can include DbRef documents in results. Can skip/limit results.",
        Examples = new string[] {
            "db.orders.select",
            "db.orders.select $ where _id > 100",
            "db.orders.select { name: $.name, age: $.age - 2017 } where age < 30 limit 100",
            "db.orders.select { name: UPPER($.name), mobile: FIRST($.phones[@.type = 'Mobile'].Number) }",
            "db.orders.select $ into new_orders where DATEDIFF('day', $.orderDate, DATE()) = 0",
            "db.orders.select $ include $.customer, $.produts[*] where _id = 22"
        }
    )]
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

            // support into new_collection
            var into = s.Scan(@"\s*into\s+([\w-]+)", 1);
            var autoId = BsonType.ObjectId;

            // checks for autoId
            if (into.Length > 0)
            {
                var sid = s.Scan(@"\s+_?id:(int32|int64|int|long|objectid|datetime|date|guid)", 1).Trim().ToLower();
                autoId =
                    sid == "int32" || sid == "int" ? BsonType.Int32 :
                    sid == "int64" || sid == "long" ? BsonType.Int64 :
                    sid == "date" || sid == "datetime" ? BsonType.DateTime :
                    sid == "guid" ? BsonType.Guid : BsonType.ObjectId;
            }

            if (s.Scan(@"\s*where\s*").Length > 0)
            {
                query = this.ReadQuery(s, true);
            }

            var skipLimit = this.ReadSkipLimit(s);
            var includes = this.ReadIncludes(s);

            s.ThrowIfNotFinish();

            var docs = engine.Find(col, query, includes, skipLimit.Key, skipLimit.Value);

            if (into.Length > 0)
            {
                // insert into results to other collection collection
                var count = engine.InsertBulk(into, this.Execute(docs, expression), autoId: autoId);

                // return inserted documents
                return new BsonValue[] { count };
            }
            else
            {
                return this.Execute(docs, expression).Select(x => x as BsonValue);
            }
        }

        private IEnumerable<BsonDocument> Execute(IEnumerable<BsonDocument> docs, BsonExpression expression)
        {
            foreach (var doc in docs)
            {
                foreach (var value in expression.Execute(doc, false))
                {
                    if (value.IsDocument)
                    {
                        yield return value.AsDocument;
                    }
                    else
                    {
                        yield return new BsonDocument { ["expr"] = value };
                    }
                }
            }
        }
    }
}