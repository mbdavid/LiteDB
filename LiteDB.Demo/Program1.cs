using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LiteDB;

namespace litedb_test
{
    internal class Program
    {
        public class EntityInt { public int Id { get; set; } public string Name { get; set; } }


        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The args.</param>
        [STAThread]
        static void Main(string[] args)
        {
            var r = new { name = "John", Age = 40 };

            var d = BsonMapper.Global.ToDocument(r);

            var j = JsonSerializer.Serialize(d);

            File.Delete("d:\\Test.db");
            var db = new LiteDatabase("d:\\Test.db");
            var col = db.GetCollection<EntityInt>("col1");
            for (int i = 0; i < 50; i++)
                col.Upsert(new EntityInt { Name = i.ToString() });

            for (int i = 0; i < 10; i++)
                col.Delete(i);

            db.Shrink();

            for (int i = 0; i < 5; i++)
                col.Upsert(new EntityInt { Name = i.ToString() }); //Cannot insert duplicate key in unique index '_id'. The duplicate value is '42'.

        }
    }


}