using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LiteDB;

namespace litedb_test
{
    /// <summary>
    /// Test record from desktop app
    /// </summary>

    public class UnreadNotificationRecord
    {
        public enum NotificationTypeEnum
        {
            Info,
            Error
        }

        [BsonId]
        public int Id { get; set; }

        public Guid UserId { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationTypeEnum NotificationType { get; set; }

        public DateTime When { get; set; }
    }



    /// <summary>
    /// 
    /// </summary>

    internal class Program1
    {

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The args.</param>
        [STAThread]
        static void Main0(string[] args)
        {
            /*
             * Important!: 
             * `connectionString`
             * has to be path to **NON-SSD** drive
             */

            const string connectionString = "d://generated.litedb";

            //Some GUID keys to share across all processes
            Guid[] sharedGuids = {
                Guid.Parse("B9321547-D4BE-461F-B7F9-2E2600839428"),
                Guid.Parse("1F0689E8-121A-414D-80D1-2A54B516A6AC")
            };

            // main start point
            if (args.Length == 0)
            {
                File.Delete(connectionString);

                const int processCount = 15;

                Console.WriteLine($"Spawning {processCount} child processes");

                for (int i = 0; i < processCount; i++)
                    Process.Start(Process.GetCurrentProcess().MainModule.FileName, $"child_{i}");

                return;
            }

            var procId = args[0];

            Console.WriteLine($"Running as `{procId}`");

            try
            {
                using (var database = new LiteDatabase(connectionString))
                {

                    database.Shrink();

                    var collection = database.GetCollection<UnreadNotificationRecord>();
                    collection.EnsureIndex(x => x.UserId);

                    for (int i = 0; i < 50; i++)
                    {
                        var random = new Random();

                        var record = new UnreadNotificationRecord
                        {
                            UserId = sharedGuids[random.Next() % sharedGuids.Length],
                        };

                        Console.WriteLine($"Item[{i}]: {procId}");

                        //Every 2nd iteration run some query that actually has to yield some results

                        if (i % 2 == 0)
                            collection.
                                Find(Query.EQ(nameof(UnreadNotificationRecord.UserId), sharedGuids[random.Next() % sharedGuids.Length])).
                                ToArray();

                        //Every iteration insert new record

                        collection.Insert(record);
                    }

                    Console.WriteLine($"{procId} process finished");

                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.ReadKey();
            }
        }
    }


}