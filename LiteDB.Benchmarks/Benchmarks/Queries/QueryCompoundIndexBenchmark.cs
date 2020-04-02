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
    public class QueryCompoundIndexBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => Constants.DatabaseNames.QUERIES;

        private const string _compoundIndexName = "CompoundIndex1";

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }

        [GlobalSetup(Target = nameof(Query_SimpleIndex_Baseline))]
        public void GlobalSetupSimpleIndexBaseline()
        {
            DatabaseInstance = new LiteDatabase(ConnectionString);
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.IsFavorite);

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [GlobalSetup(Target = nameof(Query_CompoundIndexVariant))]
        public void GlobalSetupCompoundIndexVariant()
        {
            DatabaseInstance = new LiteDatabase(ConnectionString);
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(_compoundIndexName, $"$.{nameof(FileMetaBase.IsFavorite)};$.{nameof(FileMetaBase.ShouldBeShown)}");

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [Benchmark(Baseline = true)]
        public List<FileMetaBase> Query_SimpleIndex_Baseline()
        {
            return _fileMetaCollection.Find(Query.And(
                    Query.EQ(nameof(FileMetaBase.IsFavorite), false),
                    Query.EQ(nameof(FileMetaBase.ShouldBeShown), true)))
                .ToList();
        }

        [Benchmark]
        public List<FileMetaBase> Query_CompoundIndexVariant()
        {
            return _fileMetaCollection.Find(Query.EQ(_compoundIndexName, $"{false};{true}")).ToList();
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