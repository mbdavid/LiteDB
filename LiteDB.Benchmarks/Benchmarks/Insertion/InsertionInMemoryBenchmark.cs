using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Insertion
{
    public class InsertionInMemoryDatabaseBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => @"Insertion.db";

        private List<FileMetaBase> data;

        private LiteDatabase DatabaseInstanceNormal { get; set; }
        private LiteDatabase DatabaseInstanceInMemory { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaNormalCollection { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaInMemoryCollection { get; set; }

        [GlobalSetup(Target = nameof(InsertionNormal))]
        public void GlobalSetupNormal()
        {
            data = FileMetaGenerator<FileMetaBase>.GenerateList(N); // executed once per each N value

            DatabaseInstanceNormal = new LiteDatabase(new ConnectionString(DatabasePath) {Mode = FileMode.Exclusive});
            _fileMetaNormalCollection = DatabaseInstanceNormal.GetCollection<FileMetaBase>();
        }

        [GlobalSetup(Target = nameof(InsertionInMemory))]
        public void GlobalSetupInMemory()
        {
            data = FileMetaGenerator<FileMetaBase>.GenerateList(N); // executed once per each N value

            DatabaseInstanceInMemory = new LiteDatabase(new MemoryStream());
            _fileMetaInMemoryCollection = DatabaseInstanceInMemory.GetCollection<FileMetaBase>();
        }

        [Benchmark(Baseline = true)]
        public int InsertionNormal()
        {
            return _fileMetaNormalCollection.Insert(data);
        }

        [Benchmark]
        public int InsertionInMemory()
        {
            return _fileMetaInMemoryCollection.Insert(data);
        }

        [IterationCleanup(Target = nameof(InsertionNormal))]
        public void CleanUpNormal()
        {
            const string collectionName = nameof(FileMetaBase);

            var droppedCollectionIndexes = DatabaseInstanceNormal.GetCollection(collectionName).GetIndexes().ToList();
            DatabaseInstanceNormal.DropCollection(collectionName);

            foreach (var indexInfo in droppedCollectionIndexes)
            {
                DatabaseInstanceNormal.Engine.EnsureIndex(collectionName, indexInfo.Field, indexInfo.Expression);
            }
        }

        [IterationCleanup(Target = nameof(InsertionInMemory))]
        public void CleanUpInMemory()
        {
            const string collectionName = nameof(FileMetaBase);

            var droppedCollectionIndexes = DatabaseInstanceInMemory.GetCollection(collectionName).GetIndexes().ToList();
            DatabaseInstanceInMemory.DropCollection(collectionName);

            foreach (var indexInfo in droppedCollectionIndexes)
            {
                DatabaseInstanceInMemory.Engine.EnsureIndex(collectionName, indexInfo.Field, indexInfo.Expression);
            }
        }

        [GlobalCleanup(Target = nameof(InsertionNormal))]
        public void GlobalCleanupNormal()
        {
            _fileMetaNormalCollection = null;

            DatabaseInstanceNormal.DropCollection(nameof(FileMetaBase));
            DatabaseInstanceNormal.Dispose();
        }

        [GlobalCleanup(Target = nameof(InsertionInMemory))]
        public void GlobalCleanupInMemory()
        {
            _fileMetaInMemoryCollection = null;

            DatabaseInstanceInMemory.DropCollection(nameof(FileMetaBase));
            DatabaseInstanceInMemory.Dispose();
        }
    }
}