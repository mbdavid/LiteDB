using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Deletion
{
    [BenchmarkCategory(Constants.Categories.DELETION)]
    public class DeletionBenchmark : BenchmarkBase
    {
        private List<FileMetaBase> data;

        private ILiteCollection<FileMetaBase> _fileMetaCollection;

        [GlobalSetup]
        public void GlobalSetup()
        {
            File.Delete(DatabasePath);

            DatabaseInstance = new LiteDatabase(ConnectionString());
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(file => file.IsFavorite);
            _fileMetaCollection.EnsureIndex(file => file.ShouldBeShown);

            data = FileMetaGenerator<FileMetaBase>.GenerateList(DatasetSize);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _fileMetaCollection.Insert(data);
            DatabaseInstance.Checkpoint();
        }

        [Benchmark(Baseline = true)]
        public int DeleteAllExpression()
        {
            var count = _fileMetaCollection.DeleteMany("1 = 1");
            DatabaseInstance.Checkpoint();
            return count;
        }

        [Benchmark]
        public void DropCollectionAndRecreate()
        {
            const string collectionName = nameof(FileMetaBase);

            var indexesCollection = DatabaseInstance.GetCollection("$indexes");
            var droppedCollectionIndexes = indexesCollection.Query().Where(x => x["collection"] == collectionName && x["name"] != "_id").ToDocuments().ToList();

            DatabaseInstance.DropCollection(collectionName);

            foreach (var indexInfo in droppedCollectionIndexes)
            {
                DatabaseInstance.GetCollection(collectionName).EnsureIndex(indexInfo["name"], BsonExpression.Create(indexInfo["expression"]), indexInfo["unique"]);
            }

            DatabaseInstance.Checkpoint();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
            DatabaseInstance.DropCollection(nameof(FileMetaBase));
            DatabaseInstance.Checkpoint();
            DatabaseInstance.Dispose();

            File.Delete(DatabasePath);
        }
    }
}