using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class TestMemoryFile
    {
        static string PATH = @"D:\memory-file.db";
        static int N0 = 100;
        static int N1 = 10000;
        static BsonDocument doc = new BsonDocument
        {
            ["_id"] = 1,
            ["name"] = "NoSQL Database",
            ["birthday"] = new DateTime(1977, 10, 30),
            ["phones"] = new BsonArray { "000000", "12345678" },
            ["active"] = true
        }; // 109b

        static int DOC_SIZE = doc.GetBytesCount(true);

        public static void Run(Stopwatch sw)
        {
            File.Delete(PATH);
           
            var factory = new FileStreamFactory(PATH, false);
            var pool = new StreamPool(factory, DbFileMode.Logfile);
            var file = new MemoryFile(pool, null);
            
            Console.WriteLine("Processing... " + (N0 * N1));
            
            sw.Start();
            
            // Write documents inside data file (append)
            WriteFile(file);
            
            Console.WriteLine("Write: " + sw.ElapsedMilliseconds);

            // dispose - re-open test with no memory cache
            file.Dispose();
            file = new MemoryFile(pool, null);
            
            Thread.Sleep(2000);
            sw.Restart();
            
            ReadFile(file);
            
            Console.WriteLine("Read: " + sw.ElapsedMilliseconds);
            
            file.Dispose();
            pool.Dispose();

        }

        static void ReadFile(MemoryFile file)
        {
            var fileReader = file.GetReader(false);

            IEnumerable<ArraySlice<byte>> source()
            {
                var pos = 0;

                while (pos < file.Length)
                {
                    var page = fileReader.GetPage(pos);

                    pos += 8192;

                    yield return page;
                }
            };

            for (var j = 0; j < N0; j++)
            {
                using (var bufferReader = new BufferReader(source()))
                {
                    for (var i = 0; i < N1; i++)
                    {
                        var d = bufferReader.ReadDocument();
                    }
                }

                fileReader.ReleasePages();
            }

            fileReader.Dispose();
        }

        static void WriteFile(MemoryFile file)
        {
            var fileReader = file.GetReader(true);

            var dirtyPages = new List<PageBuffer>();

            IEnumerable<ArraySlice<byte>> source()
            {
                while (true)
                {
                    var page = fileReader.NewPage();
                    dirtyPages.Add(page);
                    yield return page;
                }
            };

            var bufferWriter = new BufferWriter(source());
            {
                for (var j = 0; j < N0; j++)
                {
                    for (var i = 0; i < N1; i++)
                    {
                        doc["_id"] = j * i;

                        bufferWriter.WriteDocument(doc);
                    }

                    // middle process writes
                    file.WriteAsync(dirtyPages);
                    fileReader.ReleasePages();

                    dirtyPages.Clear();
                }
            }

            fileReader.Dispose();
        }
    }
}
