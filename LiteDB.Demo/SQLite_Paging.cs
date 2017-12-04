using LiteDB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    class SQLite_Paging : ITest
    {
        static string filename = Path.Combine(Path.GetTempPath(), "sqlite_paging.db");

        private SQLiteConnection _db = null;

        public void Init()
        {
            File.Delete(filename);

            _db = new SQLiteConnection("Data Source=" + filename);
            _db.Open();
        }

        public void Populate(IEnumerable<string[]> lines)
        {
            var table = new SQLiteCommand("CREATE TABLE col (id INTEGER NOT NULL PRIMARY KEY, name TEXT, age INT)", _db);
            table.ExecuteNonQuery();

            // create indexes before
            var idxAge = new SQLiteCommand("CREATE INDEX idx_age ON col (age)", _db);
            idxAge.ExecuteNonQuery();

            using (var trans = _db.BeginTransaction())
            {
                var cmd = new SQLiteCommand("INSERT INTO col (id, name, age) VALUES (@id, @name, @age)", _db);

                cmd.Parameters.Add(new SQLiteParameter("id", DbType.Int32));
                cmd.Parameters.Add(new SQLiteParameter("name", DbType.String));
                cmd.Parameters.Add(new SQLiteParameter("age", DbType.Int32));

                foreach (var line in lines)
                {
                    cmd.Parameters["id"].Value = Convert.ToInt32(line[0]);
                    cmd.Parameters["name"].Value = line[1];
                    cmd.Parameters["age"].Value = Convert.ToInt32(line[2]);

                    cmd.ExecuteNonQuery();
                }

                trans.Commit();
            }
        }

        /// <summary>
        /// Count query reading all data
        /// </summary>
        public long Count()
        {
            var cmd = new SQLiteCommand("SELECT * FROM col WHERE age = 22", _db);

            var count = 0;

            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    count++;
                }
            }

            return Convert.ToInt64(count);
        }

        public List<string> Fetch(int skip, int limit)
        {
            var cmd = new SQLiteCommand(
                @"SELECT * 
                    FROM col 
                   WHERE age = 22 
                   ORDER BY name
                   LIMIT " + limit + " OFFSET " + skip, _db);

            var result = new List<string>();

            using (var reader = cmd.ExecuteReader())
            {
                while(reader.Read())
                {
                    result.Add(
                        reader["id"].ToString().PadRight(6) + " - " +
                        reader["name"].ToString().PadRight(30) + "  -> " +
                        reader["age"].ToString());
                }
            }

            return result;
        }
    }
}