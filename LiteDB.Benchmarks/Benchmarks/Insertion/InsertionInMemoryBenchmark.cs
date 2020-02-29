using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Insertion
{
    [BenchmarkCategory(Constants.Categories.INSERTION)]
    public class InsertionInMemoryBenchmark : BenchmarkBase
    {
        private List<FileMetaBase> data;

        private ILiteDatabase DatabaseInstanceNormal { get; set; }
        private ILiteDatabase DatabaseInstanceInMemory { get; set; }
        private ILiteCollection<FileMetaBase> _fileMetaNormalCollection { get; set; }
        private ILiteCollection<FileMetaBase> _fileMetaInMemoryCollection { get; set; }

        [GlobalSetup(Target = nameof(InsertionNormal))]
        public void GlobalSetupNormal()
        {
            File.Delete(DatabasePath);

            data = FileMetaGenerator<FileMetaBase>.GenerateList(DatasetSize); // executed once per each N value

            DatabaseInstanceNormal = new LiteDatabase(ConnectionString());
            _fileMetaNormalCollection = DatabaseInstanceNormal.GetCollection<FileMetaBase>();
        }

        [GlobalSetup(Target = nameof(InsertionInMemory))]
        public void GlobalSetupInMemory()
        {
            data = FileMetaGenerator<FileMetaBase>.GenerateList(DatasetSize); // executed once per each N value

            DatabaseInstanceInMemory = new LiteDatabase(new MemoryStream());
            _fileMetaInMemoryCollection = DatabaseInstanceInMemory.GetCollection<FileMetaBase>();
        }

        [Benchmark(Baseline = true)]
        public int InsertionNormal()
        {
            var count = _fileMetaNormalCollection.Insert(data);
            DatabaseInstanceNormal.Checkpoint();
            return count;
        }

        [Benchmark]
        public int InsertionInMemory()
        {
            var count = _fileMetaInMemoryCollection.Insert(data);
            DatabaseInstanceNormal.Checkpoint();
            return count;
        }

        [IterationCleanup(Target = nameof(InsertionNormal))]
        public void CleanUpNormal()
        {
            const string collectionName = nameof(FileMetaBase);

            var indexesCollection = DatabaseInstanceNormal.GetCollection("$indexes");
            var droppedCollectionIndexes = indexesCollection.Query().Where(x => x["collection"] == collectionName && x["name"] != "_id").ToDocuments().ToList();

            DatabaseInstanceNormal.DropCollection(collectionName);

            foreach (var indexInfo in droppedCollectionIndexes)
            {
                DatabaseInstanceNormal.GetCollection(collectionName).EnsureIndex(indexInfo["name"], BsonExpression.Create(indexInfo["expression"]), indexInfo["unique"]);
            }
            DatabaseInstanceNormal.Checkpoint();
            DatabaseInstanceNormal.Rebuild();
        }

        [IterationCleanup(Target = nameof(InsertionInMemory))]
        public void CleanUpInMemory()
        {
            const string collectionName = nameof(FileMetaBase);

            var indexesCollection = DatabaseInstanceInMemory.GetCollection("$indexes");
            var droppedCollectionIndexes = indexesCollection.Query().Where(x => x["collection"] == collectionName && x["name"] != "_id").ToDocuments().ToList();

            DatabaseInstanceInMemory.DropCollection(collectionName);

            foreach (var indexInfo in droppedCollectionIndexes)
            {
                DatabaseInstanceInMemory.GetCollection(collectionName).EnsureIndex(indexInfo["name"], BsonExpression.Create(indexInfo["expression"]), indexInfo["unique"]);
            }
            DatabaseInstanceInMemory.Checkpoint();
            DatabaseInstanceInMemory.Rebuild();
        }

        [GlobalCleanup(Target = nameof(InsertionNormal))]
        public void GlobalCleanupNormal()
        {
            _fileMetaNormalCollection = null;

            DatabaseInstanceNormal.DropCollection(nameof(FileMetaBase));
            DatabaseInstanceNormal.Checkpoint();
            DatabaseInstanceNormal.Dispose();

            File.Delete(DatabasePath);
        }

        [GlobalCleanup(Target = nameof(InsertionInMemory))]
        public void GlobalCleanupInMemory()
        {
            _fileMetaInMemoryCollection = null;

            DatabaseInstanceInMemory.DropCollection(nameof(FileMetaBase));
            DatabaseInstanceInMemory.Checkpoint();
            DatabaseInstanceInMemory.Dispose();
        }
    }
}