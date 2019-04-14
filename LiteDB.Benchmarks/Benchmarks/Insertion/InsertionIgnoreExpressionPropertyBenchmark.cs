using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Insertion
{
    public class InsertionIgnoreExpressionPropertyDatabaseBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => @"Insertion.db";

        private List<FileMetaBase> baseData;
        private List<FileMetaWithExclusion> baseDataWithBsonIgnore;

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }
        private LiteCollection<FileMetaWithExclusion> _fileMetaExclusionCollection { get; set; }

        [GlobalSetup(Target = nameof(Insertion))]
        public void GlobalBsonIgnoreSetup()
        {
            DatabaseInstance = new LiteDatabase(new ConnectionString(DatabasePath) {Mode = FileMode.Exclusive});
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            baseData = FileMetaGenerator<FileMetaBase>.GenerateList(N); // executed once per each N value
        }

        [GlobalSetup(Target = nameof(InsertionWithBsonIgnore))]
        public void GlobalIgnorePropertySetup()
        {
            DatabaseInstance = new LiteDatabase(new ConnectionString(DatabasePath) {Mode = FileMode.Exclusive});
            _fileMetaExclusionCollection = DatabaseInstance.GetCollection<FileMetaWithExclusion>();
            _fileMetaExclusionCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            baseDataWithBsonIgnore = FileMetaGenerator<FileMetaWithExclusion>.GenerateList(N); // executed once per each N value
        }

        [Benchmark(Baseline = true)]
        public int Insertion()
        {
            return _fileMetaCollection.Insert(baseData);
        }

        [Benchmark]
        public int InsertionWithBsonIgnore()
        {
            return _fileMetaExclusionCollection.Insert(baseDataWithBsonIgnore);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            var collectionNames = DatabaseInstance.GetCollectionNames().ToList();
            foreach (var collectionName in collectionNames)
            {
                var droppedCollectionIndexes = DatabaseInstance.GetCollection(collectionName).GetIndexes().ToList();
                DatabaseInstance.DropCollection(collectionName);

                foreach (var indexInfo in droppedCollectionIndexes)
                {
                    DatabaseInstance.Engine.EnsureIndex(collectionName, indexInfo.Field, indexInfo.Expression);
                }
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            baseData?.Clear();
            baseData = null;

            baseDataWithBsonIgnore?.Clear();
            baseDataWithBsonIgnore = null;

            DatabaseInstance.DropCollection(nameof(FileMetaBase));
            DatabaseInstance.DropCollection(nameof(FileMetaWithExclusion));
            DatabaseInstance.Dispose();
        }
    }
}