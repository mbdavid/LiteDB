using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class TestScript
    {
        static Random RND = new Random();
        static string PATH = @"D:\script-vf.db";
        static string PATH_LOG = @"D:\script-vf-log.db";
        static string SQL = @"-- importa 70.000 na col1
select {_id:int(_id), name, address, city} 
into col1
from $file_csv({filename:'d:/_app-db/datagen.txt', delimiter:';'})
limit 70000;

-- exclui metade da col1
delete col1
where _id < 50000;

-- importa mais 20.000 na col2
select {_id:int(_id), name, address, city} 
into col2
from $file_csv({filename:'d:/_app-db/datagen.txt', delimiter:';'})
limit 20000;

-- limpa metade da col2
delete col2
where _id > 10000;

-- um insert bem simples na col3
insert into col3 values {_id: 'minha coleção 3!!' };

-- apaga tudo na col1 e col2
delete col1 where _id > 0;
delete col2 where _id > 0;

-- um insert bem simples na col4
insert into col4 values {_id: 'minha coleção 4!!' };

-- apaga todo resto
delete col3 where _id != null;
delete col4 where _id != null;

-- reaproveita todo disco
select {_id:int(_id), name, address, city} 
into col5
from $file_csv({filename:'d:/_app-db/datagen.txt', delimiter:';'})
limit 150000;";

        public static void Run(Stopwatch sw)
        {
            File.Delete(PATH);
            File.Delete(PATH_LOG);

            var connection = new ConnectionString
            {
                Filename = PATH,
                CheckpointOnShutdown = true
            };

            sw.Start();

            using (var db = new LiteDatabase(connection))
            {

                using (var r = new StringReader(SQL))
                {
                    while(r.Peek() >= 0)
                    {
                        db.Execute(r);
                    }
                }
            }
            sw.Stop();

        }
    }
}
