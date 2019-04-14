using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Queries
{
    public class QueryAllBenchmark : BenchmarkBase
    {
        protected override string DatabasePath => @"Query.db";

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            DatabaseInstance = new LiteDatabase(new ConnectionString(DatabasePath) {Mode = FileMode.Exclusive});
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value
        }

        [Benchmark(Baseline = true)]
        public List<FileMetaBase> FindAll()
        {
            return _fileMetaCollection.FindAll().ToList();
        }

        [Benchmark]
        public List<FileMetaBase> FindAllWithExpression()
        {
            return _fileMetaCollection.Find(_ => true).ToList();
        }

        [Benchmark]
        public List<FileMetaBase> FindAllWithQuery()
        {
            return _fileMetaCollection.Find(Query.All()).ToList();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            // Disposing logic
            DatabaseInstance.DropCollection(nameof(FileMetaBase));
            DatabaseInstance.Dispose();

            File.Delete("");
        }
    }
}