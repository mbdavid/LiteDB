using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Insertion
{
    [BenchmarkCategory(Constants.Categories.INSERTION)]
    public class MultiThreadedInsertionBenchmark
    {
        // Insertion data size
        [Params(10, 50, 100, 500, 1000, 5000 , 10000)]
        public int DatasetSize;

        [Params(null, "SecurePassword")]
        public string Password;

        [Params(1, 2, 5, 10, 20)]
        public int AmountOfThreads;

        private List<FileMetaBase> _data;

        private string DatabasePath => Constants.DATABASE_NAME;
        private LiteDatabase DatabaseInstance { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            File.Delete(DatabasePath);

            DatabaseInstance = new LiteDatabase(new ConnectionString(DatabasePath)
            {
                Connection = ConnectionType.Direct,
                Password = Password
            });

            // Attempt to initialize the engine before benchmarks start running
            DatabaseInstance.GetCollectionNames();

            _data = FileMetaGenerator<FileMetaBase>.GenerateList(DatasetSize); // executed once for each benchmark run
        }

        private Action[] _insertionTasksArray;

        [IterationSetup]
        public void IterationSetupGeneric()
        {
            _insertionTasksArray = new Action[AmountOfThreads];
            for (var i = 0; i < AmountOfThreads; i++)
            {
                var collectionName = ((char) (65 + i)).ToString();
                _insertionTasksArray[i] = () => DatabaseInstance.GetCollection<FileMetaBase>(collectionName).Insert(_data);
            }
        }

        [Benchmark]
        public void InsertionYThreads()
        {
            Parallel.Invoke(_insertionTasksArray);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            for (var i = 0; i < _insertionTasksArray.Length; i++)
            {
                _insertionTasksArray[i] = null;
            }

            _insertionTasksArray = null;

            foreach (var collectionName in DatabaseInstance.GetCollectionNames())
            {
                DatabaseInstance.DropCollection(collectionName);
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            foreach (var collectionName in DatabaseInstance.GetCollectionNames())
            {
                DatabaseInstance.DropCollection(collectionName);
            }

            DatabaseInstance.Checkpoint();
            DatabaseInstance?.Dispose();
            DatabaseInstance = null;

            File.Delete(DatabasePath);
        }
    }
}