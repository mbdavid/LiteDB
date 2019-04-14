using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Queries
{
    public class QueryWithDateTimeOffsetDatabaseBenchmark : DatabaseBenchmarkBase
    {
        protected override string DatabasePath => @"Query.db";

        private DateTime _dateTimeConstraint;
        private BsonValue _dateTimeConstraintBsonValue;

        private LiteDatabase DatabaseInstance { get; set; }
        private LiteCollection<FileMetaBase> _fileMetaCollection { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            DatabaseInstance = new LiteDatabase(ConnectionString);
            _fileMetaCollection = DatabaseInstance.GetCollection<FileMetaBase>();
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ValidFrom);
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ValidTo);
            _fileMetaCollection.EnsureIndex(fileMeta => fileMeta.ShouldBeShown);

            _fileMetaCollection.Insert(FileMetaGenerator<FileMetaBase>.GenerateList(N)); // executed once per each N value

            _dateTimeConstraint = DateTime.Now;
            _dateTimeConstraintBsonValue = new BsonValue(DateTime.Now);
        }

        [Benchmark(Baseline = true)]
        public List<FileMetaBase> Expression_Normal_Baseline()
        {
            return _fileMetaCollection.Find(fileMeta => (fileMeta.ValidFrom > _dateTimeConstraint || fileMeta.ValidTo < _dateTimeConstraint) && fileMeta.ShouldBeShown).ToList();
        }

        [Benchmark]
        public List<FileMetaBase> Query_Normal()
        {
            return _fileMetaCollection.Find(Query.And(
                    Query.Or(
                        Query.GT(nameof(FileMetaBase.ValidFrom), _dateTimeConstraintBsonValue),
                        Query.LT(nameof(FileMetaBase.ValidTo), _dateTimeConstraintBsonValue)),
                    Query.EQ(nameof(FileMetaBase.ShouldBeShown), true)))
                .ToList();
        }

        [Benchmark]
        public List<FileMetaBase> Expression_ParametersSwitched()
        {
            return _fileMetaCollection.Find(fileMeta => fileMeta.ShouldBeShown && (fileMeta.ValidFrom > _dateTimeConstraint || fileMeta.ValidTo < _dateTimeConstraint)).ToList();
        }

        [Benchmark]
        public List<FileMetaBase> Query_ParametersSwitched()
        {
            return _fileMetaCollection.Find(Query.And(
                    Query.EQ(nameof(FileMetaBase.ShouldBeShown), true),
                    Query.Or(
                        Query.GT(nameof(FileMetaBase.ValidFrom), _dateTimeConstraintBsonValue),
                        Query.LT(nameof(FileMetaBase.ValidTo), _dateTimeConstraintBsonValue))))
                .ToList();
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