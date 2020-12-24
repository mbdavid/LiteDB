using System;
using LiteDB;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace CorruptionTest
{
    class Program
    {
        const string ORIGINAL_FILE_PATH = @"..\..\..\PixelDesktop-v5.db";
        const string TEMP_FILE_PATH = @"..\..\..\PixelDesktop-v5-TEMP.db";
        const string TEMP_LOG_FILE_PATH = @"..\..\..\PixelDesktop-v5-TEMP-log.db";
        public static ConnectionString _connection = new ConnectionString
        {
            Filename = TEMP_FILE_PATH,
            Password = "NR8vrRMgTY2BUfUtUtm9"
        };
        static void Main(string[] args)
        {
            File.Delete(TEMP_FILE_PATH);
            File.Delete(TEMP_LOG_FILE_PATH);

            File.Copy(ORIGINAL_FILE_PATH, TEMP_FILE_PATH);

            using var desktopUploaderLiteDB = new LiteDatabase(_connection);
            var _localJobCollection = desktopUploaderLiteDB.GetCollection<Objects.job>("local_jobs", BsonAutoId.Int64);

            var obj = _localJobCollection.FindById(1);

            var maxId = _localJobCollection.Max("$._id").AsInt64;

            var rnd = new Random(322);

            var i = 0;

            var ops = new List<string>(8192);

            while (i < 10000)
            {
                if (i >= 119)
                {
                    ; //this is when corruption happens, add a breakpoint here
                }
                var f = rnd.NextDouble();
                if (f < 0.8D)
                {
                    var id = (long)rnd.Next(1, (int)maxId);
                    if (id == 5)
                    {
                        ;
                    }
                    var upd = _localJobCollection.FindById(id);
                    upd.event_log.Add(upd.event_log.Last());
                    _localJobCollection.Update(upd);
                    ops.Add("U-" + id.ToString());
                }
                else
                {
                    obj.litedb_id = ++maxId;
                    var insertedId = _localJobCollection.Insert(obj);
                    ops.Add("I-" + insertedId.AsInt64.ToString());
                }

                var id5 = _localJobCollection.FindById(5L);
                if (id5.lookup_metadata.Keys.ToList()[4].Length > 40)
                {
                    ; //entry was corrupted, add a breakpoint here
                }
                i++;
            }
        }
    }
}
