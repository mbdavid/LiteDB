using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Queries
{
    public class QueryIndexDatabaseBenchmarks : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => Constants.DatabaseNames.Queries;

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }

        [GlobalSetup(Targets = new[] {nameof(FindWithExpression), nameof(FindWithQuery)})]
        public void GlobalSetup()
        {
            DatabaseInstance = new LiteDatabase(ConnectionString);
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [GlobalSetup(Targets = new[] {nameof(FindWithIndexExpression), nameof(FindWithIndexQuery)})]
        public void GlobalIndexSetup()
        {
            DatabaseInstance = new LiteDatabase(ConnectionString);
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.IsFavorite);
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [Benchmark(Baseline = true)]
        public List<FileMetaBase> FindWithExpression()
        {
            return _fileMetaCollection.Find(fileMeta => fileMeta.IsFavorite).ToList();
        }

        [Benchmark]
        public List<FileMetaBase> FindWithQuery()
        {
            return _fileMetaCollection.Find(Query.EQ(nameof(FileMetaBase.IsFavorite), true)).ToList();
        }

        [Benchmark]
        public List<FileMetaBase> FindWithIndexExpression()
        {
            return _fileMetaCollection.Find(fileMeta => fileMeta.IsFavorite).ToList();
        }

        [Benchmark]
        public List<FileMetaBase> FindWithIndexQuery()
        {
            return _fileMetaCollection.Find(Query.EQ(nameof(FileMetaBase.IsFavorite), true)).ToList();
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