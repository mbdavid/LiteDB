using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Queries
{
    public class QueryIgnoreExpressionPropertiesDatabaseBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => @"Query.db";

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }
        private LiteCollection<FileMetaWithExclusion> _fileMetaExclusionCollection { get; set; }

        [GlobalSetup(Target = nameof(DeserializeBaseline))]
        public void GlobalSetup()
        {
            DatabaseInstance = new LiteDatabase(new ConnectionString(DatabasePath) {Mode = FileMode.Exclusive});
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [GlobalSetup(Target = nameof(DeserializeWithIgnore))]
        public void GlobalIndexSetup()
        {
            DatabaseInstance = new LiteDatabase(new ConnectionString(@"Query.db") {Mode = FileMode.Exclusive});
            _fileMetaExclusionCollection = DatabaseInstance.GetCollection<FileMetaWithExclusion>();
            _fileMetaExclusionCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            _fileMetaExclusionCollection.Insert(FileMetaGenerator<FileMetaWithExclusion>.GenerateList(N)); // executed once per each N value
        }

        [Benchmark(Baseline = true)]
        public List<FileMetaBase> DeserializeBaseline()
        {
            return _fileMetaCollection.Find(fileMeta => fileMeta.ShouldBeShown).ToList();
        }

        [Benchmark]
        public List<FileMetaWithExclusion> DeserializeWithIgnore()
        {
            return _fileMetaExclusionCollection.Find(fileMeta => fileMeta.ShouldBeShown).ToList();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
            DatabaseInstance.DropCollection(nameof(FileMetaBase));
            _fileMetaCollection = null;

            DatabaseInstance.DropCollection(nameof(FileMetaWithExclusion));
            _fileMetaExclusionCollection = null;

            DatabaseInstance.Dispose();
        }
    }
}