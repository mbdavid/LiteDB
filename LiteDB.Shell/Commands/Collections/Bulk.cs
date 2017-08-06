using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class CollectionBulk : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "bulk");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var filename = s.Scan(@".*");

            using (var sr = new StreamReader(new FileStream(filename, System.IO.FileMode.Open)))
            {
                var docs = JsonSerializer.DeserializeArray(sr);

                env.Display.WriteResult(env.Engine.Insert(col, docs.Select(x => x.AsDocument)));
            }
        }
    }
}
