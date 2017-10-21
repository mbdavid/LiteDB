using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
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
                var count = engine.Insert(col, value.AsArray.RawValue.Select(x => x.AsDocument), autoId);

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