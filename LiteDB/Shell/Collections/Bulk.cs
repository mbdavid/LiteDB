using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    internal class CollectionBulk : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "bulk");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var filename = s.Scan(@".*");

            using (var sr = new StreamReader(new FileStream(filename, System.IO.FileMode.Open)))
            {
                var docs = JsonSerializer.DeserializeArray(sr);

                yield return engine.Insert(col, docs.Select(x => x.AsDocument));
            }
        }
    }
}
