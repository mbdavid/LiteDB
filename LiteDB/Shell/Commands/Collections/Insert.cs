using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class CollectionInsert : BaseCollection, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "insert");
        }

        public BsonValue Execute(DbEngine engine, StringScanner s)
        {
            var col = this.ReadCollection(engine, s);
            var value = JsonSerializer.Deserialize(s);

            if (value.IsArray)
            {
                return engine.Insert(col, value.AsArray.RawValue.Select(x => x.AsDocument));
            }
            else
            {
                engine.Insert(col, new BsonDocument[] { value.AsDocument });

                return BsonValue.Null;
            }
        }
    }
}