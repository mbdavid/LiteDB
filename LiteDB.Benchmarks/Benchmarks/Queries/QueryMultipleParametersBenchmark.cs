using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Queries
{
    [BenchmarkCategory(Constants.Categories.QUERIES)]
    public class QueryMultipleParametersBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => Constants.DatabaseNames.QUERIES;

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            DatabaseInstance = new LiteDatabase(ConnectionString);
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.IsFavorite);
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [Benchmark(Baseline = true)]
        public List<FileMetaBase> Expression_Normal_Baseline()
        {
            return _fileMetaCollection.Find(fileMeta => fileMeta.IsFavorite && fileMeta.ShouldBeShown).ToList();
        }

        [Benchmark]
        public List<FileMetaBase> Query_Normal()
        {
            return _fileMetaCollection.Find(Query.And(
                    Query.EQ(nameof(FileMetaBase.IsFavorite), true),
                    Query.EQ(nameof(FileMetaBase.ShouldBeShown), true)))
                .ToList();
        }

        [Benchmark]
        public List<FileMetaBase> Expression_ParametersSwitched()
        {
            return _fileMetaCollection.Find(fileMeta => fileMeta.ShouldBeShown && fileMeta.IsFavorite).ToList();
        }

        [Benchmark]
        public List<FileMetaBase> Query_ParametersSwitched()
        {
            return _fileMetaCollection.Find(Query.And(
                    Query.EQ(nameof(FileMetaBase.ShouldBeShown), true),
                    Query.EQ(nameof(FileMetaBase.IsFavorite), true)))
                .ToList();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
            DatabaseInstance.DropCollection(nameof(FileMetaBase));
            DatabaseInstance.Dispose();

            File.Delete(DatabasePath);
        }
    }
}