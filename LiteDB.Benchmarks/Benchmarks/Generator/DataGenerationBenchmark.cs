using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Benchmarks.Base;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Generator
{
    public class DataGenerationDatabaseBenchmark : BenchmarkBase
    {
        [Benchmark]
        public List<FileMetaBase> DataGeneration()
        {
            return FileMetaGenerator<FileMetaBase>.GenerateList(N);
        }

        [Benchmark]
        public List<FileMetaWithExclusion> DataWithExclusionsGeneration()
        {
            return FileMetaGenerator<FileMetaWithExclusion>.GenerateList(N);
        }
    }
}