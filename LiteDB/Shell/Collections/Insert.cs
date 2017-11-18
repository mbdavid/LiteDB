using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection",
        Name = "insert",
        Syntax = "db.<collection>.insert <jsonDoc> [id:[int|long|date|guid|objectId]]",
        Description = "Insert a new document inside collection. Can define auto id data type that will be used when _id missing in document.",
        Examples = new string[] {
            "db.customers.insert { _id: 1, name: \"John\" }",
            "db.customers.insert { name: \"Carlos\" } id:int",
            "db.customers.insert { name: \"July\", birthDate: { $date: \"2011-08-10\" } } id:guid"
        }
    )]
    internal class CollectionInsert : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "insert");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var value = JsonSerializer.Deserialize(s);
            var sid = s.Scan(@"\s+_?id:(int32|int64|int|long|objectid|datetime|date|guid)", 1).Trim().ToLower();
            var autoId =
                sid == "int32" || sid == "int" ? BsonType.Int32 :
                sid == "int64" || sid == "long" ? BsonType.Int64 :
                sid == "date" || sid == "datetime" ? BsonType.DateTime :
                sid == "guid" ? BsonType.Guid : BsonType.ObjectId;

            s.ThrowIfNotFinish();

            if (value.IsArray)
            {
                var count = engine.InsertBulk(col, value.AsArray.RawValue.Select(x => x.AsDocument), autoId: autoId);

                yield return count;
            }
            else if(value.IsDocument)
            {
                engine.Insert(col, new BsonDocument[] { value.AsDocument }, autoId);

                yield return value.AsDocument["_id"];
            }
            else
            {
                throw LiteException.SyntaxError(s, "Invalid JSON value (must be a document or an array)");
            }
        }
    }
}