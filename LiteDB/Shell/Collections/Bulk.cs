using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    [Help(
        Category = "Collection", 
        Name = "bulk",
        Syntax = "db.<collection>.bulk <filename>",
        Description = "Bulk insert a json file with documents. Json file must be an array with documents. Returns number of document inserted.",
        Examples = new string[] {
            "db.orders.bulk C:/Temp/orders.json"
        }
    )]
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

                yield return engine.InsertBulk(col, docs.Select(x => x.AsDocument));
            }
        }
    }
}
