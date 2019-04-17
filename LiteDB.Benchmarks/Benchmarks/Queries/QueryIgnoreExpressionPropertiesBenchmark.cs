using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Queries
{
    [BenchmarkCategory(LiteDB.Benchmarks.Constants.Categories.QUERIES)]
    public class QueryIgnoreExpressionPropertiesBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => Constants.DatabaseNames.Queries;

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }
        private LiteCollection<FileMetaWithExclusion> _fileMetaExclusionCollection { get; set; }

        [GlobalSetup(Target = nameof(DeserializeBaseline))]
        public void GlobalSetup()
        {
            DatabaseInstance = new LiteDatabase(ConnectionString);
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [GlobalSetup(Target = nameof(DeserializeWithIgnore))]
        public void GlobalIndexSetup()
        {
            DatabaseInstance = new LiteDatabase(ConnectionString);
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

            File.Delete(DatabasePath);
        }
    }
}