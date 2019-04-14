using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Insertion
{
    public class InsertionBasicDatabaseBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => @"Insertion.db";

        private List<FileMetaBase> data;

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            DatabaseInstance = new LiteDatabase(new ConnectionString(DatabasePath) {Mode = FileMode.Exclusive});
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();

            data = FileMetaGenerator<FileMetaBase>.GenerateList(N); // executed once per each N value
        }

        [Benchmark(Baseline = true)]
        public int Insertion()
        {
            return _fileMetaCollection.Insert(data);
        }

        [Benchmark]
        public int InsertionBulk()
        {
            return _fileMetaCollection.InsertBulk(data);
        }

        [Benchmark]
        public void InsertionWithLoop()
        {
            for (var i = 0; i < data.Count; i++)
            {
                _fileMetaCollection.Insert(data[i]);
            }
        }

        [Benchmark]
        public int Upsertion()
        {
            return _fileMetaCollection.Upsert(data);
        }

        [Benchmark]
        public void UpsertionWithLoop()
        {
            for (var i = 0; i < data.Count; i++)
            {
                _fileMetaCollection.Upsert(data[i]);
            }
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            const string collectionName = nameof(FileMetaBase);

            var droppedCollectionIndexes = DatabaseInstance.GetCollection(collectionName).GetIndexes().ToList();
            DatabaseInstance.DropCollection(collectionName);

            foreach (var indexInfo in droppedCollectionIndexes)
            {
                DatabaseInstance.Engine.EnsureIndex(collectionName, indexInfo.Field, indexInfo.Expression);
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            DatabaseInstance.DropCollection(nameof(FileMetaBase));
            DatabaseInstance.Dispose();
            // Disposing logic
        }
    }
}