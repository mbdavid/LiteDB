using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionBulk : BaseCollection, ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "bulk");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var col = this.ReadCollection(engine, s);
            var filename = s.Scan(@".*");

            using (var sr = new StreamReader(new FileStream(filename, System.IO.FileMode.Open)))
            {
                var docs = JsonSerializer.DeserializeArray(sr);

                display.WriteResult(engine.Insert(col, docs.Select(x => x.AsDocument)));
            }
        }
    }
}
