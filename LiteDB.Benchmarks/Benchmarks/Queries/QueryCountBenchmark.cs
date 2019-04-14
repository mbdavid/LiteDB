using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Queries
{
    public class QueryCountDatabaseBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => @"Query.db";

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            DatabaseInstance = new LiteDatabase(new ConnectionString(DatabasePath) {Mode = FileMode.Exclusive});
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [Benchmark(Baseline = true)]
        public int CountWithLinq()
        {
            return _fileMetaCollection.Find(Query.EQ(nameof(FileMetaBase.ShouldBeShown), true)).Count();
        }

        [Benchmark]
        public int CountWithExpression()
        {
            return _fileMetaCollection.Count(fileMeta => fileMeta.ShouldBeShown);
        }

        [Benchmark]
        public int CountWithQuery()
        {
            return _fileMetaCollection.Count(Query.EQ(nameof(FileMetaBase.ShouldBeShown), true));
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
            DatabaseInstance.DropCollection(nameof(FileMetaBase));
            DatabaseInstance.Dispose();
        }
    }
}